using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Patient;
using HospitalMS.DAL.Models.PatientModule;
using Microsoft.EntityFrameworkCore;

namespace HospitalMS.BLL.Service.Implementation
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PatientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<PatientListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var result = await _unitOfWork.Patients.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<PatientListItemViewModel>
            {
                Items = result.Items.Select(MapToListItem),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<List<Patient>> SearchAsync(string? searchTerm, CancellationToken ct)
        {
            return await _unitOfWork.Patients.SearchAsync(searchTerm, ct);
        }

        public async Task<List<PatientListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Patients.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(MapToListItem)
                .ToList();
        }

        public async Task<UpdatePatientViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id, ct);

            if (patient is null || patient.IsDeleted)
                return null;

            return new UpdatePatientViewModel
            {
                Id = patient.Id,
                PatientName = patient.Name,
                Gender = patient.Gender,
                Address = patient.Address,
                Country = patient.Country,
                City = patient.City,
                Notes = patient.Notes,
                NationalId = patient.NationalId
            };
        }

        public async Task<PatientDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id, ct);

            if (patient is null || patient.IsDeleted)
                return null;

            return new PatientDetailsViewModel
            {
                Id = patient.Id,
                PatientName = patient.Name,
                Gender = patient.Gender,
                Address = patient.Address,
                Country = patient.Country,
                City = patient.City,
                Notes = patient.Notes,
                NationalId = PatientDisplayHelpers.MaskNationalId(patient.NationalId) ,
                CreatedAt = patient.CreatedAt
            };
        }

        public async Task<ServiceResult<Guid>> CreateAsync(NewPatientViewModel vm, string createdBy, CancellationToken ct)
        {
            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, null, ct);
            if (!nationalIdCheck.Succeeded)
                return ServiceResult<Guid>.Failure(nationalIdCheck.Errors);

            try
            {
                var patient = new Patient(
                    vm.PatientName,
                    vm.NationalId,
                    vm.Gender,
                    vm.Address,
                    vm.Country,
                    vm.City,
                    vm.Notes);

                await _unitOfWork.Patients.AddAsync(patient, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(patient.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<Guid>.Failure("National ID already exists and must be unique.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(UpdatePatientViewModel vm, string updatedBy, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(vm.Id, ct);

            if (patient is null || patient.IsDeleted)
                return ServiceResult.Failure("Patient not found.");

            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, vm.Id, ct);
            if (!nationalIdCheck.Succeeded)
                return nationalIdCheck;

            try
            {
                patient.UpdateInfo(
                    vm.PatientName,
                    vm.Gender,
                    vm.Address,
                    vm.Country,
                    vm.City,
                    vm.Notes);

                await _unitOfWork.Patients.UpdateAsync(patient, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Failure("National ID already exists and must be unique.");
            }
        }

        public async Task<ServiceResult> AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id, ct);

            if (patient is null || patient.IsDeleted)
                return ServiceResult.Failure("Patient not found.");

            try
            {
                await _unitOfWork.Patients.AssignUserAsync(id, userId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id, ct);

            if (patient is null || patient.IsDeleted)
                return ServiceResult.Failure("Patient not found.");

            try
            {
                await _unitOfWork.Patients.DeleteAsync(id, deletedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> patientIds, CancellationToken ct)
        {
            return await _unitOfWork.Patients.GetNamesByIdsAsync(patientIds, ct);
        }

        // ---------- Helpers ----------

        private static PatientListItemViewModel MapToListItem(Patient p) => new()
        {
            Id = p.Id,
            UserId = p.UserId,
            PatientName = p.Name,
            NationalId = p.NationalId,
            Gender = p.Gender,
            Address = p.Address,
            Country = p.Country,
            City = p.City,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            IsDeleted = p.IsDeleted
        };

        private async Task<ServiceResult> ValidateNationalIdAsync(
            string? nationalId, Guid? currentId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
                return ServiceResult.Failure("National ID must be exactly 14 digits.");

            var exists = await _unitOfWork.Patients.ExistsByNationalIdAsync(nationalId, currentId, ct);

            if (exists)
                return ServiceResult.Failure("National ID already exists and must be unique.");

            return ServiceResult.Success();
        }
    }
}
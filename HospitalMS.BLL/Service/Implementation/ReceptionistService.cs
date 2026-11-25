using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Receptionist;
using HospitalMS.DAL.Models.ReceptionistModule;

namespace HospitalMS.BLL.Service.Implementation
{
    public class ReceptionistService : IReceptionistService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReceptionistService( IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<ReceptionistListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var result = await _unitOfWork.Receptionists.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<ReceptionistListItemViewModel>
            {
                Items = result.Items.Select(r => new ReceptionistListItemViewModel
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Name = r.Name,
                    NationalId = r.NationalId,
                    HireDate = r.HireDate,
                    Salary = r.Salary,
                    CreatedAt = r.CreatedAt,
                    IsDeleted = r.IsDeleted
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<List<ReceptionistListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Receptionists.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(x => new ReceptionistListItemViewModel
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Name = x.Name,
                    NationalId = x.NationalId,
                    HireDate = x.HireDate,
                    Salary = x.Salary,
                    CreatedAt = x.CreatedAt,
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public async Task<UpdateReceptionistViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var receptionist = await _unitOfWork.Receptionists.GetByIdAsync(id, ct);

            if (receptionist is null || receptionist.IsDeleted)
                return null;

            return new UpdateReceptionistViewModel
            {
                Id = receptionist.Id,
                Name = receptionist.Name,
                NationalId = receptionist.NationalId,
                HireDate = receptionist.HireDate,
                Salary = receptionist.Salary
            };
        }

        public async Task<ReceptionistDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var receptionist = await _unitOfWork.Receptionists.GetByIdAsync(id, ct);

            if (receptionist is null || receptionist.IsDeleted)
                return null;

            return new ReceptionistDetailsViewModel
            {
                Id = receptionist.Id,
                UserId = receptionist.UserId,
                Name = receptionist.Name,
                NationalId = receptionist.NationalId,
                HireDate = receptionist.HireDate,
                Salary = receptionist.Salary,
                CreatedAt = receptionist.CreatedAt
            };
        }

        public async Task<ServiceResult<Guid>> CreateAsync(
            NewReceptionistViewModel vm, string createdBy, CancellationToken ct)
        {
            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, null, ct);
            if (!nationalIdCheck.Succeeded)
            {
                return ServiceResult<Guid>.Failure(nationalIdCheck.Errors);
            }

            try
            {
                var receptionist = new Receptionist(vm.Name, vm.NationalId, vm.HireDate, vm.Salary);

                await _unitOfWork.Receptionists.AddAsync(receptionist, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(receptionist.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
        }

        public async Task<Receptionist?> GetByNationalIdAsync(string nationalId, CancellationToken ct)
        {
            return await _unitOfWork.Receptionists.GetByNationalIdAsync(nationalId, ct);
        }

        public async Task<ServiceResult> UpdateAsync(
            UpdateReceptionistViewModel vm, string updatedBy, CancellationToken ct)
        {
            var receptionist = await _unitOfWork.Receptionists.GetByIdAsync(vm.Id, ct);

            if (receptionist is null || receptionist.IsDeleted)
                return ServiceResult.Failure("Receptionist not found.");

            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, vm.Id, ct);
            if (!nationalIdCheck.Succeeded)
            {
                return nationalIdCheck;
            }

            try
            {
                receptionist.UpdateInfo(vm.Name, vm.NationalId, vm.Salary);

                await _unitOfWork.Receptionists.UpdateAsync(receptionist, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> AssignUserAsync(
            Guid id, string userId, string updatedBy, CancellationToken ct)
        {
            var receptionist = await _unitOfWork.Receptionists.GetByIdAsync(id, ct);

            if (receptionist is null || receptionist.IsDeleted)
                return ServiceResult.Failure("Receptionist not found.");

            try
            {
                await _unitOfWork.Receptionists.AssignUserAsync(id, userId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var receptionist = await _unitOfWork.Receptionists.GetByIdAsync(id, ct);

            if (receptionist is null || receptionist.IsDeleted)
                return ServiceResult.Failure("Receptionist not found.");

            try
            {
                await _unitOfWork.Receptionists.DeleteAsync(id, deletedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<List<ReceptionistUnAssignedUserViewModel>> GetAllUnassignedAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Receptionists.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted && x.UserId == null)
                .Select(x => new ReceptionistUnAssignedUserViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList();
        }

        private async Task<ServiceResult> ValidateNationalIdAsync(
            string? nationalId, Guid? currentId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
            {
                return ServiceResult.Failure("National ID must be exactly 14 digits.");
            }

            var existing = await _unitOfWork.Receptionists.GetByNationalIdAsync(nationalId, ct);

            if (existing != null && existing.Id != currentId)
            {
                return ServiceResult.Failure("National ID already exists and must be unique.");
            }

            return ServiceResult.Success();
        }
    }
}
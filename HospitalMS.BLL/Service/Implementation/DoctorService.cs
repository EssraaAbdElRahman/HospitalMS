using HospitalMS.BLL.ModelVM.Doctor;
using HospitalMS.BLL.ModelVM.DoctorSchedule;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.DAL.Models.DoctorModule;
using Microsoft.EntityFrameworkCore;
namespace HospitalMS.BLL.Service.Implementation
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PagedResult<DoctorListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var result = await _unitOfWork.Doctors.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<DoctorListItemViewModel>
            {
                Items = result.Items.Select(doctor => new DoctorListItemViewModel
                {
                    Id = doctor.Id,
                    UserId = doctor.UserId,
                    Name = doctor.Name,
                    Bio = doctor.Bio,
                    SpecialtyId = doctor.SpecialtyId,
                    SpecialtyName = doctor.Specialty.Name,
                    YearsOfExperience = doctor.YearsOfExperience,
                    DepartmentId = doctor.DepartmentId,
                    DepartmentName = doctor.Department.Name,
                    NationalId = doctor.NationalId,
                    HireDate = doctor.HireDate,
                    Salary = doctor.Salary,
                    CreatedAt = doctor.CreatedAt,
                    IsDeleted = doctor.IsDeleted
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<DoctorDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetDetailsByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return null;

            return new DoctorDetailsViewModel
            {
                Id = doctor.Id,
                Name = doctor.Name,
                NationalId = doctor.NationalId,
                Bio = doctor.Bio,
                DepartmentName = doctor.Department.Name,
                SpecialtyName = doctor.Specialty.Name,
                YearsOfExperience = doctor.YearsOfExperience,
                HireDate = doctor.HireDate,
                Salary = doctor.Salary,
                CreatedAt = doctor.CreatedAt,
                Schedules = MapSchedules(doctor)
            };
        }

        public async Task<List<DoctorListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Doctors.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(x => new DoctorListItemViewModel
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Name = x.Name,
                    Bio = x.Bio,
                    SpecialtyId = x.SpecialtyId,
                    YearsOfExperience = x.YearsOfExperience,
                    DepartmentId = x.DepartmentId,
                    NationalId = x.NationalId,
                    HireDate = x.HireDate,
                    Salary = x.Salary,
                    CreatedAt = x.CreatedAt,
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public async Task<UpdateDoctorViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return null;

            return new UpdateDoctorViewModel
            {
                Id = doctor.Id,
                Name = doctor.Name,
                Bio = doctor.Bio,
                SpecialtyId = doctor.SpecialtyId,
                YearsOfExperience = doctor.YearsOfExperience,
                DepartmentId = doctor.DepartmentId,
                HireDate = doctor.HireDate,
                Salary = doctor.Salary,
                NationalId = doctor.NationalId,
                DepartmentName = doctor.Department.Name,
                Schedules = MapSchedules(doctor)
            };
        }

        public async Task<ServiceResult<Guid>> CreateAsync(NewDoctorViewModel vm, string createdBy, CancellationToken ct)
        {
            var requiredCheck = ValidateRequiredFields(
                vm.SpecialtyId, vm.DepartmentId, vm.YearsOfExperience, vm.HireDate, vm.Salary);
            if (!requiredCheck.Succeeded)
            {
                return ServiceResult<Guid>.Failure(requiredCheck.Errors);
            }

            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, null, ct);
            if (!nationalIdCheck.Succeeded)
            {
                return ServiceResult<Guid>.Failure(nationalIdCheck.Errors);
            }

            try
            {
                var doctor = new Doctor(
                    vm.Name,
                    vm.UserId,
                    vm.NationalId,
                    vm.Bio,
                    vm.SpecialtyId!.Value,
                    vm.YearsOfExperience!.Value,
                    vm.DepartmentId!.Value,
                    vm.HireDate!.Value,
                    vm.Salary!.Value);

                await _unitOfWork.Doctors.AddAsync(doctor, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(doctor.Id);
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

        public async Task<ServiceResult> UpdateAsync(UpdateDoctorViewModel vm, string updatedBy, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(vm.Id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            var requiredCheck = ValidateRequiredFields(
                vm.SpecialtyId, vm.DepartmentId, vm.YearsOfExperience, vm.HireDate, vm.Salary);
            if (!requiredCheck.Succeeded)
            {
                return requiredCheck;
            }

            var nationalIdCheck = await ValidateNationalIdAsync(vm.NationalId, vm.Id, ct);
            if (!nationalIdCheck.Succeeded)
            {
                return nationalIdCheck;
            }

            try
            {
                doctor.UpdateInfo(
                    vm.Name,
                    vm.NationalId,
                    vm.Bio,
                    vm.SpecialtyId!.Value,
                    vm.YearsOfExperience!.Value,
                    vm.DepartmentId!.Value,
                    vm.HireDate!.Value,
                    vm.Salary!.Value);

                await _unitOfWork.Doctors.UpdateAsync(doctor, updatedBy, ct);
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

        public async Task<ServiceResult> ChangeSalaryAsync(Guid id, decimal salary, string updatedBy, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            try
            {
                await _unitOfWork.Doctors.ChangeSalaryAsync(id, salary, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> ChangeDepartmentAsync(Guid id, Guid departmentId, string updatedBy, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            try
            {
                await _unitOfWork.Doctors.ChangeDepartmentAsync(id, departmentId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> ChangeSpecialtyAsync(Guid id, Guid specialtyId, string updatedBy, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            try
            {
                await _unitOfWork.Doctors.ChangeSpecialtyAsync(id, specialtyId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            try
            {
                await _unitOfWork.Doctors.AssignUserAsync(id, userId, updatedBy, ct);
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
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id, ct);

            if (doctor is null || doctor.IsDeleted)
                return ServiceResult.Failure("Doctor not found.");

            try
            {
                await _unitOfWork.DoctorSchedules.DeleteByDoctorIdAsync(id, deletedBy, ct);
                await _unitOfWork.Doctors.DeleteAsync(id, deletedBy, ct);

                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<List<DoctorUnAssignedUserViewModel>> GetAllUnassignedAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Doctors.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted && x.UserId == null)
                .Select(x => new DoctorUnAssignedUserViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList();
        }

        public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> doctorIds, CancellationToken ct)
        {
            return await _unitOfWork.Doctors.GetNamesByIdsAsync(doctorIds, ct);
        }

        public async Task<Guid?> GetByUserIdAsync(string userId, CancellationToken ct)
        {
            var doctor = await _unitOfWork.Doctors.GetByUserIdAsync(userId, ct);

            return doctor?.Id;
        }
        // ---------- Helpers ----------
        private List<DoctorScheduleListItemViewModel> MapSchedules(Doctor doctor)
        {
            return doctor.DoctorSchedules
                .Where(x => !x.IsDeleted)
                .Select(x => new DoctorScheduleListItemViewModel
                {
                    Id = x.Id,
                    Days = x.Days,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .ToList();
        }

        private static ServiceResult ValidateRequiredFields(Guid? specialtyId, Guid? departmentId, int? yearsOfExperience, DateTime? hireDate, decimal? salary)
        {
            if (!specialtyId.HasValue)
                return ServiceResult.Failure("Specialty is required.");

            if (!departmentId.HasValue)
                return ServiceResult.Failure("Department is required.");

            if (!yearsOfExperience.HasValue)
                return ServiceResult.Failure("Years of experience is required.");

            if (!hireDate.HasValue)
                return ServiceResult.Failure("Hire date is required.");

            if (!salary.HasValue)
                return ServiceResult.Failure("Salary is required.");

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateNationalIdAsync(string? nationalId, Guid? currentId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
            {
                return ServiceResult.Failure("National ID must be exactly 14 digits.");
            }

            var existing = await _unitOfWork.Doctors.GetByNationalIdAsync(nationalId, ct);

            if (existing != null && existing.Id != currentId)
            {
                return ServiceResult.Failure("National ID already exists and must be unique.");
            }

            return ServiceResult.Success();
        }
    }
}
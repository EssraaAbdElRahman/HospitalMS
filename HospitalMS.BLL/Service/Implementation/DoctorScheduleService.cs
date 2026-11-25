using HospitalMS.BLL.ModelVM.DoctorSchedule;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.DAL.Models.DoctorModule;

namespace HospitalMS.BLL.Service.Implementation
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorScheduleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<DoctorScheduleListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var result = await _unitOfWork.DoctorSchedules.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<DoctorScheduleListItemViewModel>
            {
                Items = result.Items.Select(schedule => new DoctorScheduleListItemViewModel
                {
                    Id = schedule.Id,
                    Days = schedule.Days,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    DoctorId = schedule.DoctorId,
                    DoctorName = schedule.Doctor.Name,
                    CreatedAt = schedule.CreatedAt,
                    IsDeleted = schedule.IsDeleted
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<DoctorScheduleDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var schedule = await _unitOfWork.DoctorSchedules.GetDetailsByIdAsync(id, ct);

            if (schedule is null || schedule.IsDeleted)
                return null;

            return new DoctorScheduleDetailsViewModel
            {
                Id = schedule.Id,
                Days = schedule.Days,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                DoctorId = schedule.DoctorId,
                DoctorName = schedule.Doctor.Name,
                CreatedAt = schedule.CreatedAt
            };
        }

        public async Task<List<DoctorScheduleListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.DoctorSchedules.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(x => new DoctorScheduleListItemViewModel
                {
                    Id = x.Id,
                    Days = x.Days,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    DoctorId = x.DoctorId,
                    DoctorName = x.Doctor.Name,
                    CreatedAt = x.CreatedAt,
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public async Task<UpdateDoctorScheduleViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var schedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(id, ct);

            if (schedule is null || schedule.IsDeleted)
                return null;

            var days = schedule.Days
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => Enum.TryParse<Days>(x, out var d) ? (Days?)d : null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            return new UpdateDoctorScheduleViewModel
            {
                Id = schedule.Id,
                DoctorId = schedule.DoctorId,
                Days = days,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime
            };
        }

        public async Task<List<DoctorScheduleListItemViewModel>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct)
        {
            var schedules = await _unitOfWork.DoctorSchedules.GetByDoctorIdAsync(doctorId, ct);

            return schedules
                .Where(x => !x.IsDeleted)
                .Select(schedule => new DoctorScheduleListItemViewModel
                {
                    Id = schedule.Id,
                    DoctorId = schedule.DoctorId,
                    Days = schedule.Days,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    CreatedAt = schedule.CreatedAt,
                    IsDeleted = schedule.IsDeleted
                }).ToList();
        }

        public async Task<ServiceResult<Guid>> CreateAsync(            NewDoctorScheduleViewModel vm, string createdBy, CancellationToken ct)
        {
            var validation = Validate(vm.DoctorId, vm.Days, vm.StartTime, vm.EndTime);
            if (!validation.Succeeded)
                return ServiceResult<Guid>.Failure(validation.Errors);

            var selectedDays = vm.Days.Distinct().ToList();

            var hasConflict = await _unitOfWork.DoctorSchedules.HasConflictAsync(
                selectedDays, vm.DoctorId, vm.StartTime, vm.EndTime, ct:ct);

            if (hasConflict)
                return ServiceResult<Guid>.Failure(
                    "Doctor already has a schedule during the selected days and time.");

            var days = string.Join(", ", selectedDays);

            try
            {
                var schedule = new DoctorSchedule(days, vm.StartTime, vm.EndTime, vm.DoctorId);

                await _unitOfWork.DoctorSchedules.AddAsync(schedule, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(schedule.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateAsync(UpdateDoctorScheduleViewModel vm, string updatedBy, CancellationToken ct)
        {
            var schedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(vm.Id, ct);

            if (schedule is null || schedule.IsDeleted)
                return ServiceResult.Failure("DoctorSchedule not found.");

            var validation = Validate(vm.DoctorId, vm.Days, vm.StartTime, vm.EndTime);
            if (!validation.Succeeded)
                return validation;

            var selectedDays = vm.Days.Distinct().ToList();

            var hasConflict = await _unitOfWork.DoctorSchedules.HasConflictAsync(
                selectedDays, vm.DoctorId, vm.StartTime, vm.EndTime, vm.Id, ct);

            if (hasConflict)
                return ServiceResult.Failure(
                    "Doctor already has a schedule during the selected days and time.");

            var days = string.Join(", ", selectedDays);

            try
            {
                schedule.UpdateInfo(days, vm.DoctorId, vm.StartTime, vm.EndTime);

                await _unitOfWork.DoctorSchedules.UpdateAsync(schedule, updatedBy, ct);
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
            var schedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(id, ct);

            if (schedule is null || schedule.IsDeleted)
                return ServiceResult.Failure("DoctorSchedule not found.");

            try
            {
                await _unitOfWork.DoctorSchedules.DeleteAsync(id, deletedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        // ---------- Helpers ----------
        private static ServiceResult Validate(Guid doctorId, IEnumerable<Days>? days, DateTime startTime, DateTime endTime)
        {
            if (doctorId == Guid.Empty)
                return ServiceResult.Failure("Doctor is required.");

            if (days == null || !days.Any())
                return ServiceResult.Failure("At least one day is required.");

            if (startTime == default)
                return ServiceResult.Failure("Start time is required.");

            if (endTime == default)
                return ServiceResult.Failure("End time is required.");

            if (endTime.TimeOfDay <= startTime.TimeOfDay)
                return ServiceResult.Failure("End time must be after start time.");

            return ServiceResult.Success();
        }
    }
}
using HospitalMS.BLL.ModelVM.DoctorSchedule;
using HospitalMS.BLL.ModelVM.Pagination;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IDoctorScheduleService
    {
        Task<List<DoctorScheduleListItemViewModel>> GetAllAsync( CancellationToken ct);
        Task<PagedResult<DoctorScheduleListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<UpdateDoctorScheduleViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<DoctorScheduleDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewDoctorScheduleViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateDoctorScheduleViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
        Task<List<DoctorScheduleListItemViewModel>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct);
    }
}
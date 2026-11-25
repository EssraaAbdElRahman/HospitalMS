using HospitalMS.BLL.ModelVM.Doctor;
using HospitalMS.BLL.ModelVM.Pagination;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IDoctorService
    {
        Task<List<DoctorListItemViewModel>> GetAllAsync(CancellationToken ct);
        Task<List<DoctorUnAssignedUserViewModel>> GetAllUnassignedAsync(CancellationToken ct);
        Task<PagedResult<DoctorListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<UpdateDoctorViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<DoctorDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewDoctorViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateDoctorViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
        Task<ServiceResult> ChangeSalaryAsync(Guid id, decimal salary, string updatedBy, CancellationToken ct);
        Task<ServiceResult> ChangeDepartmentAsync(Guid id, Guid departmentId, string updatedBy, CancellationToken ct);
        Task<ServiceResult> ChangeSpecialtyAsync(Guid id, Guid specialtyId, string updatedBy, CancellationToken ct);
        Task<ServiceResult> AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct);
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> doctorIds, CancellationToken ct);
        Task<Guid?> GetByUserIdAsync(string UserId, CancellationToken ct);
    }
}
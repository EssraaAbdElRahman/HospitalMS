using HospitalMS.BLL.ModelVM.Department;
using HospitalMS.BLL.ModelVM.Pagination;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IDepartmentService
    {
        Task<List<DepartmentListItemViewModel>> GetAllAsync(CancellationToken ct);
        Task<PagedResult<DepartmentListItemViewModel>> GetPagedAsync(string? search,int page,int pageSize, CancellationToken ct);
        Task<UpdateDepartmentViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<DepartmentDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewDepartmentViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateDepartmentViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);

    }
}

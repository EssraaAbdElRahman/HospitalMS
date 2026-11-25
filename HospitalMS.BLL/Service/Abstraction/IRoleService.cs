using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Role;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IRoleService
    {
        Task<List<RoleListItemViewModel>> GetAllAsync( CancellationToken ct);
        Task<List<RoleIdNameViewModel>> GetAllRolesAsync(CancellationToken ct);
        Task<ServiceResult> CreateAsync(CreateRoleViewModel vm, CancellationToken ct);
        Task<ServiceResult> EditAsync(EditRoleViewModel vm, CancellationToken ct);
        Task<PagedResult<RoleListItemViewModel>> GetPagedAsync(string? search,int page, int pageSize, CancellationToken ct);
        Task<EditRoleViewModel?> GetByIdAsync(string id, CancellationToken ct);
        Task<RoleDetailsViewModel?> GetRoleDetailsAsync(string roleId, CancellationToken ct);
        Task<List<string>> GetRoleNamesAsync(CancellationToken ct);

    }
}

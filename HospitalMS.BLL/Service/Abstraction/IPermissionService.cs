using HospitalMS.BLL.ModelVM.Permission;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IPermissionService
    {
        Task<PermissionMatrixViewModel> GetMatrixAsync(string? roleId, CancellationToken ct);
        Task<ServiceResult> UpdateMatrixAsync(string roleId, IEnumerable<string> selectedPermissionKeys, CancellationToken ct);

    }
}

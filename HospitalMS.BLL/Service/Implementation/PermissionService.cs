using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Permission;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace HospitalMS.BLL.Service.Implementation
{
    public class PermissionService : IPermissionService
    {
        private const string PermissionClaimType = "permission";
        private const string SuperAdminRole = "SuperAdmin";

        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public PermissionService(RoleManager<ApplicationRole> roleManager, IUnitOfWork unitOfWork)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public static List<ModuleGroupViewModel> BuildModules()
        {
            return BuildMatrixHelper.BuildModuleMatrix();
        }

        public async Task<PermissionMatrixViewModel> GetMatrixAsync(string? roleId, CancellationToken ct)
        {
            var viewModel = new PermissionMatrixViewModel { Modules = BuildModules(), RoleId = roleId };
            if (string.IsNullOrEmpty(roleId))
                return viewModel;

            var roles = await _roleManager.Roles
                .AsNoTracking()
                .Where(r => r.Id == roleId)
                .OrderBy(r => r.Name)
                .ToListAsync(ct);

            foreach (var role in roles)
            {
                viewModel.RoleNames.Add(role.Name!);

                var claims = await _roleManager.GetClaimsAsync(role);
                viewModel.RolePermissions[role.Name!] = claims
                    .Where(c => c.Type == PermissionClaimType)
                    .Select(c => c.Value)
                    .ToHashSet();
            }

            return viewModel;
        }

        public async Task<ServiceResult> UpdateMatrixAsync(string roleId, IEnumerable<string> selectedPermissionKeys, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role is null)
                return ServiceResult.Failure("Role not found.");

            if (role.Name == SuperAdminRole)
                return ServiceResult.Failure("SuperAdmin permissions cannot be modified.");

            var validKeys = BuildModules()
                .SelectMany(m => m.Columns.Select(c => c.Key))
                .ToHashSet();

            var selected = (selectedPermissionKeys ?? Enumerable.Empty<string>())
                .Where(validKeys.Contains)
                .ToHashSet();

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var existingClaims = (await _roleManager.GetClaimsAsync(role))
                    .Where(c => c.Type == PermissionConstants.ClaimType)
                    .ToList();

                foreach (var claim in existingClaims.Where(c => !selected.Contains(c.Value)))
                {
                    var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                    if (!removeResult.Succeeded)
                    {
                        await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                        return ServiceResult.Failure(removeResult.Errors.Select(e => e.Description));
                    }
                }

                var existingValues = existingClaims.Select(c => c.Value).ToHashSet();
                foreach (var key in selected.Where(k => !existingValues.Contains(k)))
                {
                    var addResult = await _roleManager.AddClaimAsync(
                        role, new Claim(PermissionConstants.ClaimType, key));
                    if (!addResult.Succeeded)
                    {
                        await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                        return ServiceResult.Failure(addResult.Errors.Select(e => e.Description));
                    }
                }

                await _unitOfWork.CommitTransactionAsync(ct);
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return ServiceResult.Failure("An error occurred while updating permissions.");
            }
        }
    }
}
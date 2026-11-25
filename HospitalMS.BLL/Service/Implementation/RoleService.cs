using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Role;
using HospitalMS.DAL.Database;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalMS.BLL.Service.Implementation
{
    public class RoleService : IRoleService
    {
        private static readonly string[] ProtectedRoles = { "SuperAdmin" };

        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly HospitalDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(RoleManager<ApplicationRole> roleManager, HospitalDbContext context, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult> CreateAsync(CreateRoleViewModel vm, CancellationToken ct)
        {
            var validationResult = await IsValidToCreateAsync(vm);
            if (!validationResult.Succeeded)
                return validationResult;

            var role = new ApplicationRole(vm.RoleName, vm.RoleDescription);
            var result = await _roleManager.CreateAsync(role);

            return result.Succeeded
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.Errors.Select(e => e.Description));
        }

        private async Task<ServiceResult> IsValidToCreateAsync(CreateRoleViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.RoleName))
                return ServiceResult.Failure("Role name is required.");

            var errors = new List<string>();

            if (IsProtectedRole(vm.RoleName))
                errors.Add("This role name is reserved and cannot be used.");

            if (await _roleManager.RoleExistsAsync(vm.RoleName))
                errors.Add("A role with this name already exists.");

            return errors.Count == 0
                ? ServiceResult.Success()
                : ServiceResult.Failure(errors);
        }

        public async Task<List<RoleListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync(ct);

            var userCounts = await GetUserCountsAsync(null, ct);

            return roles.Select(role => MapToListItem(role, userCounts)).ToList();
        }

        public async Task<List<string>> GetRoleNamesAsync(CancellationToken ct)
        {
            return await _roleManager.Roles
                .AsNoTracking()
                .Where(r => r.Name != null)
                .Select(r => r.Name!)
                .OrderBy(n => n)
                .ToListAsync(ct);
        }

        public async Task<ServiceResult> EditAsync(EditRoleViewModel vm, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(vm.RoleId);
            if (role is null)
                return ServiceResult.Failure("Role not found.");

            var validationResult = await IsValidToUpdateAsync(vm, role);
            if (!validationResult.Succeeded)
                return validationResult;

            role.UpdateDescription(vm.RoleDescription);

            var nameResult = await _roleManager.SetRoleNameAsync(role, vm.RoleName);
            if (!nameResult.Succeeded)
                return ServiceResult.Failure(nameResult.Errors.Select(e => e.Description));

            var result = await _roleManager.UpdateAsync(role);

            return result.Succeeded
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.Errors.Select(e => e.Description));
        }

        private async Task<ServiceResult> IsValidToUpdateAsync(EditRoleViewModel vm, ApplicationRole existingRole)
        {
            if (string.IsNullOrWhiteSpace(vm.RoleName))
                return ServiceResult.Failure("Role name is required.");

            var errors = new List<string>();

            if (IsProtectedRole(existingRole.Name) &&
                !string.Equals(vm.RoleName, existingRole.Name, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The SuperAdmin role name cannot be changed.");
            }

            if (!string.Equals(existingRole.Name, vm.RoleName, StringComparison.OrdinalIgnoreCase)
                && await _roleManager.RoleExistsAsync(vm.RoleName))
            {
                errors.Add("A role with this name already exists.");
            }

            return errors.Count == 0
                ? ServiceResult.Success()
                : ServiceResult.Failure(errors);
        }

        private static bool IsProtectedRole(string? roleName) =>
            ProtectedRoles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));

        public async Task<PagedResult<RoleListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var query = _roleManager.Roles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.Name!.Contains(search));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var roleIds = items.Select(r => r.Id).ToList();
            var userCounts = await GetUserCountsAsync(roleIds, ct);

            return new PagedResult<RoleListItemViewModel>
            {
                Items = items.Select(role => MapToListItem(role, userCounts)).ToList(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = total,
                SearchTerm = search
            };
        }

        public async Task<EditRoleViewModel?> GetByIdAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _roleManager.Roles
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(role => new EditRoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name!,
                    RoleDescription = role.Description
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<RoleIdNameViewModel>> GetAllRolesAsync(CancellationToken ct)
        {
            return await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoleIdNameViewModel
                {
                    Id = r.Id,
                    Name = r.Name!
                })
                .ToListAsync(ct);
        }

        public async Task<RoleDetailsViewModel?> GetRoleDetailsAsync(string roleId, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role is null)
                return null;

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == PermissionConstants.ClaimType)
                .Select(c => c.Value.Trim())
                .OrderBy(p => p)
                .ToList();

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

            return new RoleDetailsViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                UsersCount = usersInRole.Count,
                Permissions = permissions,
                AssignedUsers = usersInRole
                    .OrderBy(u => u.FullName)
                    .Select(u => new UserSummaryForRoleViewModel
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email!
                    }).ToList()
            };
        }

        // ---------- Helpers ----------

        private async Task<Dictionary<string, int>> GetUserCountsAsync(List<string>? roleIds, CancellationToken ct)
        {
            var query = _context.UserRoles.AsNoTracking();

            if (roleIds is not null)
                query = query.Where(ur => roleIds.Contains(ur.RoleId));

            return await query
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
        }

        private static RoleListItemViewModel MapToListItem(ApplicationRole role, Dictionary<string, int> userCounts) => new()
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            UserCount = userCounts.TryGetValue(role.Id, out var count) ? count : 0,
            IsSystemRole = IsProtectedRole(role.Name)
        };
    }
}
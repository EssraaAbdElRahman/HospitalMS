using HospitalMS.BLL.Authorization;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HospitalMS.BLL.Seeding
{
    public sealed class IdentitySeeder : IIdentitySeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public IdentitySeeder(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            var starterRoles = new[]
            {
                new ApplicationRole("SuperAdmin", "Full access to all configured permissions."),
                new ApplicationRole("Patient", "Patient account."),
                new ApplicationRole("Doctor", "Doctor staff account."),
                new ApplicationRole("Receptionist", "Receptionist staff account.")
            };

            foreach (var role in starterRoles)
            {
                if (await _roleManager.RoleExistsAsync(role.Name!))
                    continue;

                var result = await _roleManager.CreateAsync(role);
                EnsureIdentitySuccess(result, $"create role '{role.Name}'");
            }

            await AddClaimsToRole("SuperAdmin", Permissions.GetAll());
            await AddClaimsToRole("Patient", [Permissions.Appointments.Create]);
            await AddClaimsToRole("Receptionist", [
                Permissions.Receptionists.View,
                Permissions.Receptionists.Create,
                Permissions.Receptionists.Edit,
                Permissions.Receptionists.Delete,
                Permissions.Patients.View,
                Permissions.Patients.Create,
                Permissions.Patients.Edit,
                Permissions.Patients.Delete,
                Permissions.Appointments.View,
                Permissions.Appointments.Create,
                Permissions.Appointments.Edit,
                Permissions.Appointments.Delete
            ]);
        }

        private async Task AddClaimsToRole(string roleName, IEnumerable<string> permissions)
        {
            var role = await _roleManager.FindByNameAsync(roleName)
                ?? throw new InvalidOperationException($"Required role '{roleName}' was not found.");
            var existingClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var permission in permissions)
            {
                if (existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                    continue;

                var result = await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
                EnsureIdentitySuccess(result, $"add permission '{permission}' to role '{roleName}'");
            }
        }

        private static void EnsureIdentitySuccess(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Could not {operation}: {errors}");
            }
        }
    }
}

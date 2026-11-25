using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
namespace HospitalMS.PL.Authorization
{
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        public AppClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IOptions<IdentityOptions> options)
                                 : base(userManager, roleManager, options) { }
        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;

            var roleNames = await UserManager.GetRolesAsync(user);
            foreach (var roleName in roleNames)
            {
                var role = await RoleManager.FindByNameAsync(roleName);
                if (role == null)
                    continue;
                var roleClaims = await RoleManager.GetClaimsAsync(role);
                foreach (var claim in roleClaims)
                {
                    if (!identity.HasClaim(claim.Type, claim.Value))
                        identity.AddClaim(claim);
                }
            }
            return principal;
        }
    }
}

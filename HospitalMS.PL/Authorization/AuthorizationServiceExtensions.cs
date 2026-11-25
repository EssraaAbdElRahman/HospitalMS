using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalMS.PL.Authorization
{
    public static class AuthorizationServiceExtensions
    {
        public static async Task<bool> HasPermissionAsync(this IAuthorizationService authorizationService,ClaimsPrincipal user,            string permission)
        {
            var result = await authorizationService.AuthorizeAsync(user, permission);
            return result.Succeeded;
        }
    }
}

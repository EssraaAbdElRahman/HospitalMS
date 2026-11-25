using Microsoft.AspNetCore.Identity;
namespace HospitalMS.DAL.Models.AppModule
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; private set; }
        private ApplicationRole() : base() { }
        public ApplicationRole(string roleName, string? description = null) : base(roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name is required");

            Description = description;
        }
        public void UpdateDescription(string? description)
        {
            Description = description;
        }
    }
}

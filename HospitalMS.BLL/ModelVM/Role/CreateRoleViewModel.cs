
namespace HospitalMS.BLL.ModelVM.Role
{
    public class CreateRoleViewModel
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
    }
}

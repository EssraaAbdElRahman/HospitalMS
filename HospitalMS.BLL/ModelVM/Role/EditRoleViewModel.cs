
namespace HospitalMS.BLL.ModelVM.Role
{
    public class EditRoleViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        [Required]
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
    }
}


namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email is required.")]
        public string Role { get; set; } = string.Empty;
    }
}

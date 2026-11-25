namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]    
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;
        public Guid? DoctorId { get; set; }
        public Guid? ReceptionistId { get; set; }
        [Required(ErrorMessage = "User Type is required.")]
        public UserType UserType { get;  set; } 
        public StaffType? StaffType { get;  set; }    
    }
}

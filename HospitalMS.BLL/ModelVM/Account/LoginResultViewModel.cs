namespace HospitalMS.BLL.ModelVM.Account
{
    public class LoginResultViewModel
    {
        public bool IsLockedOut { get; set; }
        public UserType UserType { get; set; }
        public StaffType? StaffType { get; set; }
        public string? ReturnUrl { get; set; }
    }
}

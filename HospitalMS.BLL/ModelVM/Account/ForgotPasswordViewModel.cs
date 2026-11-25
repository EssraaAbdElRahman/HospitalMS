namespace HospitalMS.BLL.ModelVM.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "RequiredField")]
        [EmailAddress(ErrorMessage = "InvalidEmail")]
        public string Email { get; set; } = string.Empty;
        
    }
}

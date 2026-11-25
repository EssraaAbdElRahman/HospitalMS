namespace HospitalMS.BLL.ModelVM.Account
{
    public class SetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;

        public string? Email { get; set; } 
        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UsersListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public PasswordResetRequestStatus PasswordResetStatus { get; set; }
        public AccountStatus Status { get; set; }
    }
}

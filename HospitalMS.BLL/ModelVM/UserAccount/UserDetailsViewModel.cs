namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }

        public UserType UserType { get; set; }
        public StaffType? StaffType { get; set; }
        public string RoleName { get; set; } = null!;

        public AccountStatus Status { get; set; }
        public PasswordResetRequestStatus PasswordResetStatus { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedByName { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedByName { get; set; }
    }
}

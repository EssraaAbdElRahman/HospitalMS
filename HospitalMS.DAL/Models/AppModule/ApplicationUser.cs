using Microsoft.AspNetCore.Identity;

namespace HospitalMS.DAL.Models.AppModule
{
    public sealed class ApplicationUser : IdentityUser
    {
        public string FullName { get; private set; } = string.Empty;
        public AccountStatus Status { get; private set; } = AccountStatus.Pending;
        public UserType UserType { get; private set; }
        public StaffType? StaffType { get; private set; }
        public PasswordResetRequestStatus PasswordResetStatus { get; private set; } = PasswordResetRequestStatus.None;
        public bool IsAdmin { get; private set; }
        public string? CreatedByAdminId { get; private set; }
        public DateTime CreatedAtUtc { get; private set; } = DateTime.Now;
        public DateTime? UpdatedAtUtc { get; private set; }
        public string? UpdatedByAdminId { get; private set; }

        private ApplicationUser() { }

        public static ApplicationUser CreateStaff(string fullName, string email, UserType userType,
                                                    StaffType? staffType, string createdByAdminId)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            return new ApplicationUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                UserType = userType,
                StaffType = staffType,
                PasswordResetStatus = PasswordResetRequestStatus.None,
                IsAdmin = (userType == UserType.Admin),
                Status = AccountStatus.Pending,
                CreatedByAdminId = createdByAdminId,
                CreatedAtUtc = DateTime.Now
            };
        }
        public static ApplicationUser CreatePatientUser(string fullName, string email, string createdByAdminId)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(createdByAdminId))
                throw new ArgumentException("CreatedByAdminId is required");

            return new ApplicationUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                UserType = UserType.Patient,
                IsAdmin = false,
                Status = AccountStatus.Pending,
                CreatedByAdminId = createdByAdminId,
                CreatedAtUtc = DateTime.Now
            };
        }

        public void UpdateProfile(string fullName, string updatedByAdminId)
        {
            SetFullName(fullName);
            MarkUpdated(updatedByAdminId);
        }
        private void SetFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required");

            FullName = fullName;
        }
        public void UpdateForgetPasswordStatus(PasswordResetRequestStatus status, string updatedByAdminId)
        {
            PasswordResetStatus = status;
            MarkUpdated(updatedByAdminId);
        }
        public void ActivateViaLink(string updatedByAdminId)
        {
            Status = AccountStatus.Active;
            MarkUpdated(updatedByAdminId);
        }
        public void Activate(string updatedByAdminId)
        {
            Status = AccountStatus.Active;
            MarkUpdated(updatedByAdminId);
        }

        public void Suspend(string updatedByAdminId)
        {
            Status = AccountStatus.Suspended;
            MarkUpdated(updatedByAdminId);
        }
        public void SetAccountStatus(AccountStatus status, string updatedByAdminId)
        {
            Status = status;
            MarkUpdated(updatedByAdminId);
        }

        public void PromoteToAdmin(string updatedByAdminId)
        {
            IsAdmin = true;
            MarkUpdated(updatedByAdminId);
        }

        public void RevokeAdmin(string updatedByAdminId)
        {
            IsAdmin = false;
            MarkUpdated(updatedByAdminId);
        }

        private void MarkUpdated(string updatedByAdminId)
        {
            UpdatedByAdminId = updatedByAdminId;
            UpdatedAtUtc = DateTime.Now;
        }
    }
}
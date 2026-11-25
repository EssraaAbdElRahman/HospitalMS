
namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class GetUsersIndexInfoViewModel
    {
        public string? Search { get; set; }
        public UserType? UserType { get; set; }
        public StaffType? StaffType { get; set; }
        public AccountStatus? Status { get; set; }
        public PasswordResetRequestStatus? PasswordResetStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}

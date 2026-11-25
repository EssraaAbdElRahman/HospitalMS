using HospitalMS.BLL.ModelVM.Pagination;

namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UsersIndexViewModel
    {
        public UsersSummaryViewModel Summary { get; set; } = new();
        public PagedResult<UsersListItemViewModel> PagedUsers { get; set; } = new();
        public UserType? SelectedUserType { get; set; }
        public StaffType? SelectedStaffType { get; set; }
        public AccountStatus? SelectedStatus { get; set; }
        public PasswordResetRequestStatus? SelectedPasswordResetStatus { get; set; }
    }
}

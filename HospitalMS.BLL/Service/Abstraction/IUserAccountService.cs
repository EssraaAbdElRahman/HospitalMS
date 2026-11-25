using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.UserAccount;
namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IUserAccountService
    {
        Task<ServiceResult> CreateAsync(CreateUserViewModel viewModel, string createdByAdminId, CancellationToken ct);
        Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId, CancellationToken ct);
        Task<ServiceResult<GenerateInvitationLinkResultViewModel>> GenerateActivationLinkAsync(string userId, CancellationToken ct);
        Task<ServiceResult<GenerateInvitationLinkResultViewModel>> GeneratePasswordResetLinkAsync(string userId, CancellationToken ct);
        Task<ServiceResult<UpdateStatusViewModel>> GetForStatusUpdateAsync(string userId, CancellationToken ct);
        Task<ServiceResult> UpdateStatusAsync(string userId, AccountStatus newStatus, string requestingAdminId, CancellationToken ct);
        Task<UsersIndexViewModel> GetUsersIndexAsync(GetUsersIndexInfoViewModel viewModel, CancellationToken ct);
        Task<PagedResult<UsersListItemViewModel>> GetPagedAsync(GetUsersIndexInfoViewModel viewModel, CancellationToken ct);
        Task<UsersSummaryViewModel> GetUsersSummaryAsync(CancellationToken ct);
        Task<ServiceResult> EditUserAsync(EditUserViewModel viewModel, string updatedByAdminId, CancellationToken ct);
        Task<EditUserViewModel?> GetByIdAsync(string userId, CancellationToken ct);
    }
}

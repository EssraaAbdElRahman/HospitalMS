using HospitalMS.BLL.ModelVM.Account;
using HospitalMS.BLL.ModelVM.Login;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IAccountService
    {
        Task<ServiceResult<LoginResultViewModel>> LoginAsync(LoginViewModel viewModel, CancellationToken ct);
        Task<ServiceResult> LogoutAsync(CancellationToken ct);
        Task<ServiceResult> ActivateAccountAsync(SetPasswordViewModel viewModel, CancellationToken ct);
        Task<SetPasswordDetailsViewModel?> GetActivationDetailsAsync(SetPasswordDetailsViewModel viewModel, CancellationToken ct);
        Task<ServiceResult> RequestPasswordResetAsync(string email, CancellationToken ct);
        Task<SetPasswordDetailsViewModel?> GetResetPasswordDetailsAsync(SetPasswordDetailsViewModel viewModel, CancellationToken ct);
        Task<ServiceResult> ResetPasswordAsync(SetPasswordViewModel viewModel, CancellationToken ct);
    }
}
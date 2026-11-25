using HospitalMS.BLL.ModelVM.Account;
using HospitalMS.BLL.ModelVM.Login;
using HospitalMS.DAL.Models.AppModule;
using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Service.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<LoginResultViewModel>> LoginAsync(LoginViewModel viewModel, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(viewModel.Email);
            if (user is null)
            {
                return ServiceResult<LoginResultViewModel>.Failure("Invalid credentials.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, viewModel.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return ServiceResult<LoginResultViewModel>.Failure(
                    "Account is temporarily locked due to multiple failed attempts.");
            }

            if (!result.Succeeded)
            {
                return ServiceResult<LoginResultViewModel>.Failure("Invalid credentials.");
            }

            switch (user.Status)
            {
                case AccountStatus.Pending:
                    return ServiceResult<LoginResultViewModel>.Failure(
                        "Account is not activated yet.");
                case AccountStatus.OnHold:
                    return ServiceResult<LoginResultViewModel>.Failure("Account is on hold.");
                case AccountStatus.Suspended:
                    return ServiceResult<LoginResultViewModel>.Failure("Account is suspended.");
            }

            await _signInManager.SignInAsync(user, viewModel.RememberMe);

            if (user.PasswordResetStatus != PasswordResetRequestStatus.None)
            {
                user.UpdateForgetPasswordStatus(PasswordResetRequestStatus.None, "self-login");
                await _userManager.UpdateAsync(user);
            }

            var data = new LoginResultViewModel
            {
                IsLockedOut = false,
                UserType = user.UserType,
                StaffType = user.StaffType,
                ReturnUrl = viewModel.ReturnUrl
            };

            return ServiceResult<LoginResultViewModel>.Success(data);
        }

        public async Task<ServiceResult> LogoutAsync(CancellationToken ct)
        {
            await _signInManager.SignOutAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RequestPasswordResetAsync(string email, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null || user.UserType == UserType.Patient || user.Status != AccountStatus.Active)
            {
                return ServiceResult.Success();
            }

            user.UpdateForgetPasswordStatus(PasswordResetRequestStatus.Pending, "self-request-reset");
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(result.Errors.Select(e => e.Description));
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ActivateAccountAsync(SetPasswordViewModel viewModel, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(viewModel.UserId);

            if (user is null || !IsEligibleForActivation(user))
            {
                return ServiceResult.Failure("This activation link is invalid or has expired.");
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, viewModel.Token, viewModel.Password);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    return ServiceResult.Failure(result.Errors.Select(e => e.Description));
                }

                user.ActivateViaLink("self-activation");

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    return ServiceResult.Failure(updateResult.Errors.Select(e => e.Description));
                }

                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return ServiceResult.Failure("An error occurred while activating the account.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ResetPasswordAsync(SetPasswordViewModel viewModel, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(viewModel.UserId);
            if (user is null || !IsEligibleForPasswordReset(user))
            {
                return ServiceResult.Failure("This reset password link is invalid or has expired.");
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, viewModel.Token, viewModel.Password);

                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    return ServiceResult.Failure(result.Errors.Select(e => e.Description));
                }

                user.UpdateForgetPasswordStatus(PasswordResetRequestStatus.Completed, "self-reset");

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    return ServiceResult.Failure(updateResult.Errors.Select(e => e.Description));
                }

                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return ServiceResult.Failure("An error occurred while resetting the password.");
            }

            return ServiceResult.Success();
        }

        public Task<SetPasswordDetailsViewModel?> GetActivationDetailsAsync(SetPasswordDetailsViewModel viewModel, CancellationToken ct)
            => GetTokenDetailsAsync(viewModel, user => !IsEligibleForActivation(user));

        public Task<SetPasswordDetailsViewModel?> GetResetPasswordDetailsAsync(SetPasswordDetailsViewModel viewModel, CancellationToken ct)
            => GetTokenDetailsAsync(viewModel, user => !IsEligibleForPasswordReset(user));

        // ---------- Helpers ----------

        private static bool IsEligibleForActivation(ApplicationUser user)
            => user.Status == AccountStatus.Pending;

        private static bool IsEligibleForPasswordReset(ApplicationUser user)
            => user.Status == AccountStatus.Active &&
               user.PasswordResetStatus == PasswordResetRequestStatus.Pending;

        private async Task<SetPasswordDetailsViewModel?> GetTokenDetailsAsync(SetPasswordDetailsViewModel viewModel, Func<ApplicationUser, bool> isInvalidState)
        {
            var user = await _userManager.FindByIdAsync(viewModel.UserId);
            if (user is null || isInvalidState(user))
            {
                return null;
            }

            return new SetPasswordDetailsViewModel
            {
                UserId = viewModel.UserId,
                Token = viewModel.Token,
                Email = user.Email
            };
        }
    }
}
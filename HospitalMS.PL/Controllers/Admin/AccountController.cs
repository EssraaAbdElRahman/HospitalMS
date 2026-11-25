using HospitalMS.BLL.ModelVM.Account;
using HospitalMS.BLL.ModelVM.Login;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
namespace HospitalMS.PL.Controllers.Admin
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("login")]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _accountService.LoginAsync(model, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(model);
            }

            var data = result.Data!;

            if (!string.IsNullOrEmpty(data.ReturnUrl) && Url.IsLocalUrl(data.ReturnUrl))
                return Redirect(data.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            await _accountService.LogoutAsync(ct);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ActivateAccount(string userId, string token, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(InvalidActivationLink));

            var decodedToken = TryDecodeToken(token);
            if (decodedToken is null)
                return RedirectToAction(nameof(InvalidActivationLink));

            var details = await _accountService.GetActivationDetailsAsync(
                new SetPasswordDetailsViewModel { UserId = userId, Token = decodedToken }, ct);

            if (details is null)
                return RedirectToAction(nameof(InvalidActivationLink));

            var viewModel = new SetPasswordViewModel
            {
                UserId = details.UserId,
                Token = details.Token, 
                Email = details.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAccount(SetPasswordViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var result = await _accountService.ActivateAccountAsync(viewModel, ct);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors));
                return View(viewModel);
            }

            TempData["SuccessMessage"] = "Your account has been activated successfully. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult InvalidActivationLink()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _accountService.RequestPasswordResetAsync(model.Email, ct);

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId, string token, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(InvalidActivationLink));

            var decodedToken = TryDecodeToken(token);
            if (decodedToken is null)
                return RedirectToAction(nameof(InvalidActivationLink));

            var details = await _accountService.GetResetPasswordDetailsAsync(
                new SetPasswordDetailsViewModel { UserId = userId, Token = decodedToken }, ct);

            if (details is null)
                return RedirectToAction(nameof(InvalidActivationLink));

            var viewModel = new SetPasswordViewModel
            {
                UserId = details.UserId,
                Token = details.Token,
                Email = details.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(SetPasswordViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var result = await _accountService.ResetPasswordAsync(viewModel, ct);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors));
                return View(viewModel);
            }

            TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        private static string? TryDecodeToken(string encoded)
        {
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
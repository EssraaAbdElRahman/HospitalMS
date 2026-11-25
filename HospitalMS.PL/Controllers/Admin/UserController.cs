using HospitalMS.BLL.ModelVM.UserAccount;
using HospitalMS.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;

namespace HospitalMS.PL.Controllers.Admin
{
    [Authorize]
    public class UserController : Controller
    {
        private const int MaxPageSize = 50;
        private const string PatientRoleName = "Patient";

        private readonly IUserAccountService _userAccountService;
        private readonly IRoleService _roleService;
        private readonly IReceptionistService _receptionistService;
        private readonly IDoctorService _doctorService;

        public UserController(
            IUserAccountService userAccountService,
            IRoleService roleService,
            IReceptionistService receptionistService,
            IDoctorService doctorService)
        {
            _userAccountService = userAccountService;
            _roleService = roleService;
            _receptionistService = receptionistService;
            _doctorService = doctorService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

        [HttpGet]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> Index(
            UsersPageViewModel viewModel, int? page, string? search,
            bool fromDashboard = false, CancellationToken ct = default)
        {
            var data = viewModel.Info;

            if (page.HasValue)
                data.Page = page.Value;

            if (search != null)
                data.Search = search;

            if (data.Page < 1) data.Page = 1;
            if (data.PageSize < 1) data.PageSize = 7;
            if (data.PageSize > MaxPageSize) data.PageSize = MaxPageSize;

            viewModel.Users = await _userAccountService.GetUsersIndexAsync(data, ct);
            ViewBag.FromDashboard = fromDashboard;
            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Users.Create)]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadDropdowns(ct);
            return View(new CreateUserViewModel { UserType = UserType.Admin });
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ct, viewModel.Role, viewModel.ReceptionistId, viewModel.DoctorId);
                return View(viewModel);
            }

            var result = await _userAccountService.CreateAsync(viewModel, CurrentUserId, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                await LoadDropdowns(ct, viewModel.Role, viewModel.ReceptionistId, viewModel.DoctorId);
                return View(viewModel);
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> Details(string id, CancellationToken ct)
        {
            var details = await _userAccountService.GetUserDetailsAsync(id, ct);
            if (details is null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(details);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Users.Edit)]
        public async Task<IActionResult> Edit(string id, CancellationToken ct)
        {
            var user = await _userAccountService.GetByIdAsync(id, ct);
            if (user is null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownRoles(ct);
            return View(user);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownRoles(ct, viewModel.Role);
                return View(viewModel);
            }

            var result = await _userAccountService.EditUserAsync(viewModel, CurrentUserId, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                await LoadDropdownRoles(ct, viewModel.Role);
                return View(viewModel);
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Users.ManageStatus)]
        public async Task<IActionResult> GenerateActivationLink(string userId, CancellationToken ct)
        {
            var result = await _userAccountService.GenerateActivationLinkAsync(userId, ct);

            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(" ", result.Errors) });

            var link = Url.Action(
                action: "ActivateAccount",
                controller: "Account",
                values: new { userId = result.Data!.UserId, token = EncodeToken(result.Data.Token) },
                protocol: Request.Scheme,
                host: Request.Host.Value);

            return Json(new { success = true, link });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Users.ManageStatus)]
        public async Task<IActionResult> GeneratePasswordResetLink(string userId, CancellationToken ct)
        {
            var result = await _userAccountService.GeneratePasswordResetLinkAsync(userId, ct);

            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(" ", result.Errors) });

            var link = Url.Action(
                action: "ResetPassword",
                controller: "Account",
                values: new { userId = result.Data!.UserId, token = EncodeToken(result.Data.Token) },
                protocol: Request.Scheme,
                host: Request.Host.Value);

            return Json(new { success = true, link });
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Users.ManageStatus)]
        public async Task<IActionResult> UpdateStatus(string id, CancellationToken ct)
        {
            var result = await _userAccountService.GetForStatusUpdateAsync(id, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors);
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Users.ManageStatus)]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await ReloadStatusViewModelAsync(viewModel, ct);
                return View(viewModel);
            }

            var result = await _userAccountService.UpdateStatusAsync(
                viewModel.Id, viewModel.NewStatus!.Value, CurrentUserId, ct);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors));
                await ReloadStatusViewModelAsync(viewModel, ct);
                return View(viewModel);
            }

            TempData["Success"] = "Account status updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private static string EncodeToken(string token)
            => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        private async Task ReloadStatusViewModelAsync(UpdateStatusViewModel viewModel, CancellationToken ct)
        {
            var reload = await _userAccountService.GetForStatusUpdateAsync(viewModel.Id, ct);
            if (reload.Succeeded)
            {
                viewModel.FullName = reload.Data!.FullName;
                viewModel.CurrentStatus = reload.Data.CurrentStatus;
                viewModel.AllowedStatuses = reload.Data.AllowedStatuses;
            }
        }

        private async Task LoadDropdowns(
            CancellationToken ct,
            string? selectedRoleId = null,
            Guid? selectedReceptionistId = null,
            Guid? selectedDoctorId = null)
        {
            var receptionists = await _receptionistService.GetAllUnassignedAsync(ct);
            var doctors = await _doctorService.GetAllUnassignedAsync(ct);

            ViewBag.Receptionists = new SelectList(
                receptionists.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList(),
                "Value", "Text", selectedReceptionistId);

            ViewBag.Doctors = new SelectList(
                doctors.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToList(),
                "Value", "Text", selectedDoctorId);

            await LoadDropdownRoles(ct, selectedRoleId);
        }

        private async Task LoadDropdownRoles(CancellationToken ct, string? selectedRoleId = null)
        {
            var roles = await _roleService.GetAllRolesAsync(ct);

            var roleSelectList = roles
                .Where(r => r.Name != PatientRoleName)
                .Select(r => new SelectListItem { Value = r.Id, Text = r.Name })
                .ToList();

            ViewBag.Roles = new SelectList(roleSelectList, "Value", "Text", selectedRoleId);
        }
    }
}
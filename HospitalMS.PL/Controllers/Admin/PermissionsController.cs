using HospitalMS.BLL.ModelVM.Permission;

namespace HospitalMS.PL.Controllers.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class PermissionsController : Controller
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? id, CancellationToken ct)
        {
            var matrix = await _permissionService.GetMatrixAsync(id, ct);
            return View(matrix);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpdatePermissionsViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(model.RoleId))
            {
                var matrix = await _permissionService.GetMatrixAsync(model.RoleId, ct);
                return View(matrix);
            }

            var keys = (model.Selected ?? new List<string>())
                .Select(s => s.Contains('|') ? s[(s.IndexOf('|') + 1)..] : s);

            var result = await _permissionService.UpdateMatrixAsync(model.RoleId, keys, ct);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors);
                return RedirectToAction(nameof(Index), new { id = model.RoleId });
            }

            TempData["Success"] = "Permissions updated. Users must log out and back in for changes to take effect.";
            return RedirectToAction(nameof(Index), new { id = model.RoleId });
        }
    }
}
using HospitalMS.BLL.ModelVM.Role;

namespace HospitalMS.PL.Controllers.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class RoleController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 5, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _roleService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRoleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var result = await _roleService.CreateAsync(viewModel, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id, CancellationToken ct)
        {
            var role = await _roleService.GetByIdAsync(id, ct);
            if (role is null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRoleViewModel viewModel, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var result = await _roleService.EditAsync(viewModel, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id, CancellationToken ct)
        {
            var details = await _roleService.GetRoleDetailsAsync(id, ct);
            if (details is null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(details);
        }
    }
}
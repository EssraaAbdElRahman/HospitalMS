using HospitalMS.BLL.ModelVM.Department;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalMS.PL.Controllers.Department
{
    [Authorize]
    public class DepartmentController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Settings.View)]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _departmentService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Settings.View)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var department = await _departmentService.GetDetailsAsync(id, ct);

            if (department is null)
                return NotFound();

            return View(department);
        }

        [HttpGet]
        [Authorize(Policy = Settings.Manage)]
        public IActionResult Create()
        {
            return View(new NewDepartmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Create(NewDepartmentViewModel departmentVm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(departmentVm);

            var result = await _departmentService.CreateAsync(departmentVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(nameof(departmentVm.Name), error);

                return View(departmentVm);
            }

            TempData["Success"] = "Department created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var department = await _departmentService.GetByIdAsync(id, ct);

            if (department is null)
                return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Edit(UpdateDepartmentViewModel updateDepartment, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(updateDepartment);

            var result = await _departmentService.UpdateAsync(updateDepartment, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(updateDepartment);
            }

            TempData["Success"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _departmentService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Department deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }
    }
}
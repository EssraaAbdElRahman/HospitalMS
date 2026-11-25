using HospitalMS.BLL.ModelVM.Specialty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalMS.PL.Controllers.Department
{
    [Authorize]
    public class SpecialtyController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly ISpecialtyService _specialtyService;

        public SpecialtyController(ISpecialtyService specialtyService)
        {
            _specialtyService = specialtyService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Settings.View)]
        public async Task<IActionResult> Index(
            string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _specialtyService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Settings.View)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var specialty = await _specialtyService.GetDetailsAsync(id, ct);

            if (specialty is null)
                return NotFound();

            return View(specialty);
        }

        [HttpGet]
        [Authorize(Policy = Settings.Manage)]
        public IActionResult Create()
        {
            return View(new NewSpecialtyViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Create(NewSpecialtyViewModel specialtyVm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(specialtyVm);

            var result = await _specialtyService.CreateAsync(specialtyVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(nameof(specialtyVm.Name), error);

                return View(specialtyVm);
            }

            TempData["Success"] = "Specialty created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var specialty = await _specialtyService.GetByIdAsync(id, ct);

            if (specialty is null)
                return NotFound();

            return View(specialty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Edit(UpdateSpecialtyViewModel updateSpecialty, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(updateSpecialty);

            var result = await _specialtyService.UpdateAsync(updateSpecialty, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                return View(updateSpecialty);
            }

            TempData["Success"] = "Specialty updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Settings.Manage)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _specialtyService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Specialty deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }
    }
}
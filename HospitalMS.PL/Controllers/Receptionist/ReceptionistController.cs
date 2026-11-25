using HospitalMS.BLL.ModelVM.Receptionist;
namespace HospitalMS.PL.Controllers.Receptionist
{
    [Authorize]
    public class ReceptionistController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly IReceptionistService _receptionistService;

        public ReceptionistController(IReceptionistService receptionistService)
        {
            _receptionistService = receptionistService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Receptionists.View)]
        public async Task<IActionResult> Index(
            string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _receptionistService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Receptionists.View)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var receptionist = await _receptionistService.GetDetailsAsync(id, ct);

            if (receptionist is null)
                return NotFound();

            return View(receptionist);
        }

        [HttpGet]
        [Authorize(Policy = Receptionists.Create)]
        public IActionResult Create()
        {
            return View(new NewReceptionistViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Receptionists.Create)]
        public async Task<IActionResult> Create(NewReceptionistViewModel receptionistVm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(receptionistVm);

            var result = await _receptionistService.CreateAsync(receptionistVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                AddErrors(result.Errors, nameof(receptionistVm.NationalId));
                return View(receptionistVm);
            }

            TempData["Success"] = "Receptionist created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Receptionists.Edit)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var receptionist = await _receptionistService.GetByIdAsync(id, ct);

            if (receptionist is null)
                return NotFound();

            return View(receptionist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Receptionists.Edit)]
        public async Task<IActionResult> Edit(UpdateReceptionistViewModel updateReceptionist, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(updateReceptionist);

            var result = await _receptionistService.UpdateAsync(updateReceptionist, CurrentUser, ct);

            if (!result.Succeeded)
            {
                AddErrors(result.Errors, nameof(updateReceptionist.NationalId));
                return View(updateReceptionist);
            }

            TempData["Success"] = "Receptionist updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Receptionists.Delete)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _receptionistService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Receptionist deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        private void AddErrors(IEnumerable<string> errors, string nationalIdKey)
        {
            foreach (var error in errors)
            {
                if (error.Contains("National ID", StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError(nationalIdKey, error);
                else
                    ModelState.AddModelError(string.Empty, error);
            }
        }
    }
}
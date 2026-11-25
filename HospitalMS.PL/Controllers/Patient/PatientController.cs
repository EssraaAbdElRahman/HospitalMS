using HospitalMS.BLL.ModelVM.Patient;
using HospitalMS.BLL.Helper;

namespace HospitalMS.PL.Controllers.Patient
{
    [Authorize]
    public class PatientController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";


[HttpGet]
    [Authorize(Policy = Patients.View)]
    public async Task<IActionResult> Search(string? searchTerm, CancellationToken ct)
    {
        var patients = await _patientService.SearchAsync(searchTerm, ct);

        var results = patients.Select(p => new PatientSearchResultViewModel
        {
            Id = p.Id,
            PatientName = p.Name,
            NationalId = PatientDisplayHelpers.MaskNationalId(p.NationalId),
            Gender = p.Gender.ToString(),
            City = p.City
        }).ToList();

        return PartialView("_SearchResultsPartial", results);
    }

    [HttpGet]
        [Authorize(Policy = Permissions.Appointments.Create)]
        public IActionResult SelectForAppointment(Guid patientId)
        {
            return RedirectToAction("CreateByPatient", "Appointment", new { patientId });
        }

        [HttpGet]
        [Authorize(Policy = Patients.View)]
        public async Task<IActionResult> Index(
            string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _patientService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Patients.View)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var patient = await _patientService.GetDetailsAsync(id, ct);

            if (patient is null)
                return NotFound();

            return View(patient);
        }

        [HttpGet]
        [Authorize(Policy = Patients.Create)]
        public IActionResult Create()
        {
            return View(new NewPatientViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Patients.Create)]
        public async Task<IActionResult> Create(NewPatientViewModel patientVm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(patientVm);

            var result = await _patientService.CreateAsync(patientVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                AddErrors(result.Errors, nameof(patientVm.NationalId));
                return View(patientVm);
            }

            TempData["Success"] = "Patient created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Patients.Edit)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var patient = await _patientService.GetByIdAsync(id, ct);

            if (patient is null)
                return NotFound();

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Patients.Edit)]
        public async Task<IActionResult> Edit(UpdatePatientViewModel updatePatient, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(updatePatient);

            var result = await _patientService.UpdateAsync(updatePatient, CurrentUser, ct);

            if (!result.Succeeded)
            {
                AddErrors(result.Errors, nameof(updatePatient.NationalId));
                return View(updatePatient);
            }

            TempData["Success"] = "Patient updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Patients.Delete)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _patientService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Patient deleted successfully.";
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
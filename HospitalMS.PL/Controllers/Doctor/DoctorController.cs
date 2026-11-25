using HospitalMS.BLL.ModelVM.Doctor;

namespace HospitalMS.PL.Controllers.Doctor
{
    [Authorize]
    public class DoctorController : Controller
    {
        private const int MaxPageSize = 50;
        private readonly IDoctorService _doctorService;
        private readonly IDepartmentService _departmentService;
        private readonly ISpecialtyService _specialtyService;
        private readonly IDoctorScheduleService _doctorScheduleService;

        public DoctorController(
            IDoctorService doctorService,
            IDepartmentService departmentService,
            ISpecialtyService specialtyService,
            IDoctorScheduleService doctorScheduleService)
        {
            _doctorService = doctorService;
            _departmentService = departmentService;
            _specialtyService = specialtyService;
            _doctorScheduleService = doctorScheduleService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Doctors.View)]
        public async Task<IActionResult> Index(
            string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _doctorService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Doctors.View)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var doctor = await _doctorService.GetDetailsAsync(id, ct);

            if (doctor is null)
                return NotFound();

            return View(doctor);
        }

        [HttpGet]
        [Authorize(Policy = Doctors.Create)]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadDropdowns(ct);
            return View(new NewDoctorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Create)]
        public async Task<IActionResult> Create(NewDoctorViewModel doctorVm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ct, doctorVm.DepartmentId, doctorVm.SpecialtyId);
                return View(doctorVm);
            }

            var result = await _doctorService.CreateAsync(doctorVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Contains("National ID", StringComparison.OrdinalIgnoreCase))
                        ModelState.AddModelError(nameof(doctorVm.NationalId), error);
                    else if (error.Contains("Hire date", StringComparison.OrdinalIgnoreCase))
                        ModelState.AddModelError(nameof(doctorVm.HireDate), error);
                    else
                        ModelState.AddModelError(string.Empty, error);
                }

                await LoadDropdowns(ct, doctorVm.DepartmentId, doctorVm.SpecialtyId);
                return View(doctorVm);
            }

            TempData["Success"] = "Doctor created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Doctors.Edit)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var doctor = await _doctorService.GetByIdAsync(id, ct);

            if (doctor is null)
                return NotFound();

            await LoadDropdowns(ct, doctor.DepartmentId, doctor.SpecialtyId);
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Edit)]
        public async Task<IActionResult> Edit(UpdateDoctorViewModel updateDoctor, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ct, updateDoctor.DepartmentId, updateDoctor.SpecialtyId);
                return View(updateDoctor);
            }

            var result = await _doctorService.UpdateAsync(updateDoctor, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Contains("National ID", StringComparison.OrdinalIgnoreCase))
                        ModelState.AddModelError(nameof(updateDoctor.NationalId), error);
                    else if (error.Contains("Hire date", StringComparison.OrdinalIgnoreCase))
                        ModelState.AddModelError(nameof(updateDoctor.HireDate), error);
                    else
                        ModelState.AddModelError(string.Empty, error);
                }

                await LoadDropdowns(ct, updateDoctor.DepartmentId, updateDoctor.SpecialtyId);
                return View(updateDoctor);
            }

            TempData["Success"] = "Doctor updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Delete)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _doctorService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Doctor deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            CancellationToken ct, Guid? selectedDepartmentId = null, Guid? selectedSpecialtyId = null)
        {
            var departments = await _departmentService.GetAllAsync(ct);
            var specialties = await _specialtyService.GetAllAsync(ct);

            ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedDepartmentId);
            ViewBag.Specialties = new SelectList(specialties, "Id", "Name", selectedSpecialtyId);
        }
    }
}
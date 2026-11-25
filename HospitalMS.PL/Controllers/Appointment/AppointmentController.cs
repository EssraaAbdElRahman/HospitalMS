using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Appointment;
using HospitalMS.BLL.ModelVM.Patient;

namespace HospitalMS.PL.Controllers.Appointments
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(30);

        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IDepartmentService _departmentService;
        private readonly IDoctorService _doctorService;
        private readonly IDoctorScheduleService _doctorScheduleService;

        public AppointmentController(
            IPatientService patientService,
            IAppointmentService appointmentService,
            IDepartmentService departmentService,
            IDoctorService doctorService,
            IDoctorScheduleService doctorScheduleService)
        {
            _patientService = patientService;
            _appointmentService = appointmentService;
            _departmentService = departmentService;
            _doctorService = doctorService;
            _doctorScheduleService = doctorScheduleService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Permissions.Appointments.View)]
        public async Task<IActionResult> Index(
            DateTime? date, Guid? doctorId, AppointmentStatus? status, string? searchTerm,
            CancellationToken ct)
        {
            var selectedDate = date?.Date ?? DateTime.Today;

            var result = await _appointmentService.GetFilteredAsync(
                selectedDate, doctorId, status, searchTerm, ct);

            var doctors = await _doctorService.GetAllAsync(ct);
            ViewBag.Doctors = new SelectList(doctors, "Id", "Name", doctorId);

            var viewModel = new AppointmentIndexViewModel
            {
                SelectedDate = selectedDate,
                Appointments = result.Items,
                TotalCount = result.TotalItems,
                ScheduledCount = result.ScheduledCount,
                CheckedInCount = result.CheckedInCount,
                CompletedCount = result.CompletedCount,
                DoctorId = doctorId,
                Status = status,
                SearchTerm = searchTerm,
                TotalItems = result.TotalItems
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(Guid doctorId, DateTime date, CancellationToken ct)
        {
            var schedules = await _doctorScheduleService.GetByDoctorIdAsync(doctorId, ct);
            var dayName = date.DayOfWeek.ToString();

            var matching = schedules
                .Where(s => s.Days
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Contains(dayName))
                .ToList();

            if (matching.Count == 0)
                return Json(new List<string>());

            var slots = new List<DateTime>();
            foreach (var schedule in matching)
            {
                var current = date.Date.Add(schedule.StartTime.TimeOfDay);
                var end = date.Date.Add(schedule.EndTime.TimeOfDay);

                while (current.Add(SlotDuration) <= end)
                {
                    slots.Add(current);
                    current = current.Add(SlotDuration);
                }
            }

            var dayAppointments = await _appointmentService.GetByDateAsync(date.Date, ct);
            var bookedSlots = dayAppointments
                .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                .Select(a => a.AppointmentDate)
                .ToHashSet();

            var available = slots
                .Where(s => !bookedSlots.Contains(s))
                .Distinct()
                .OrderBy(s => s)
                .Select(s => s.ToString("HH:mm"))
                .ToList();

            return Json(available);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Appointments.Create)]
        public async Task<IActionResult> CreateByPatient(Guid patientId, CancellationToken ct)
        {
            var patient = await _patientService.GetByIdAsync(patientId, ct);
            if (patient is null)
                return NotFound();

            var vm = new NewAppointmentViewModel
            {
                PatientId = patient.Id,
                PatientName = patient.PatientName,
                NationalId = PatientDisplayHelpers.MaskNationalId(patient.NationalId),
                Gender = patient.Gender.ToString()
            };

            await LoadDropdownsAsync(ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Appointments.Create)]
        public async Task<IActionResult> CreateByPatient(NewAppointmentViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            var result = await _appointmentService.CreateAsync(vm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Appointments.Create)]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadDropdownsAsync(ct);
            return View(new NewAppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Appointments.Create)]
        public async Task<IActionResult> Create(NewAppointmentViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            var result = await _appointmentService.CreateAsync(vm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SearchPatients(string? term, CancellationToken ct)
        {
            var allPatients = await _patientService.GetAllAsync(ct);

            var filtered = string.IsNullOrWhiteSpace(term)
                ? allPatients
                : allPatients.Where(p =>
                    p.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.NationalId.Contains(term)).ToList();

            var result = filtered
                .Take(15)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.PatientName,
                    nationalId = PatientDisplayHelpers.MaskNationalId(p.NationalId),
                    gender = p.Gender.ToString()
                });

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreatePatient(NewPatientViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            var result = await _patientService.CreateAsync(vm, CurrentUser, ct);

            if (!result.Succeeded)
                return Json(new { success = false, errors = result.Errors });

            var patient = await _patientService.GetDetailsAsync(result.Data, ct);
            if (patient is null)
                return Json(new { success = false, errors = new[] { "Patient was created but could not be loaded." } });

            return Json(new
            {
                success = true,
                id = patient.Id,
                name = patient.PatientName,
                nationalId = PatientDisplayHelpers.MaskNationalId(patient.NationalId),
                gender = patient.Gender.ToString()
            });
        }

        [HttpGet]
        public async Task<IActionResult> PatientAppointments(Guid patientId, CancellationToken ct)
        {
            var patient = await _patientService.GetByIdAsync(patientId, ct);
            if (patient is null)
                return NotFound();

            var appointments = await _appointmentService.GetByPatientIdAsync(patientId, ct);

            ViewBag.PatientName = patient.PatientName;
            ViewBag.PatientId = patientId;

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Appointments.Delete)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, ct);
            var patientId = appointment?.PatientId;

            var result = await _appointmentService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Appointment deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            if (patientId is not null)
                return RedirectToAction(nameof(PatientAppointments), new { patientId });

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Appointments.Delete)]
        public async Task<IActionResult> DeleteApp(Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Appointment deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, ct);
            if (appointment is null)
                return NotFound();

            await LoadDropdownsAsync(ct, appointment.DepartmentId, appointment.DoctorId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateAppointmentViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            var result = await _appointmentService.UpdateAsync(vm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);

                await LoadDropdownsAsync(ct, vm.DepartmentId, vm.DoctorId);
                return View(vm);
            }

            TempData["Success"] = "Appointment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var appointment = await _appointmentService.GetByIdAsync(id, ct);
            if (appointment is null)
                return NotFound();

            var doctor = await _doctorService.GetByIdAsync(appointment.DoctorId, ct);
            appointment.DoctorName = doctor?.Name ?? "Unknown";

            return View(appointment);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsByDepartment(Guid departmentId, CancellationToken ct)
        {
            if (departmentId == Guid.Empty)
                return Json(new List<object>());

            var doctors = await _doctorService.GetAllAsync(ct);

            var filtered = doctors
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToList();

            return Json(filtered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NoShowAjax(Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.NoShowAsync(id, CurrentUser, ct);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Errors.FirstOrDefault() ?? "Failed to mark appointment as No Show."
                });
            }

            return Json(new { success = true, status = "No show" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAjax(Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.CancelAsync(id, CurrentUser, ct);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = result.Errors.FirstOrDefault() ?? "Failed to cancel appointment."
                });
            }

            return Json(new { success = true, status = "Canceled" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Appointments.Edit)]
        public async Task<IActionResult> CheckInAjax(Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.UpdateStatusAsync(id, AppointmentStatus.Check_in, CurrentUser, ct);

            if (!result.Succeeded)
                return Json(new { success = false, message = result.Errors.FirstOrDefault() ?? "Failed to check in." });

            return Json(new { success = true, status = "Check in" });
        }
        // ---------- Helpers ----------

        private async Task LoadDropdownsAsync(
            CancellationToken ct, Guid? selectedDepartmentId = null, Guid? selectedDoctorId = null)
        {
            var departments = await _departmentService.GetAllAsync(ct);
            ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedDepartmentId);

            var doctors = await _doctorService.GetAllAsync(ct);
            if (selectedDepartmentId.HasValue && selectedDepartmentId.Value != Guid.Empty)
            {
                doctors = doctors.Where(d => d.DepartmentId == selectedDepartmentId.Value).ToList();
            }

            ViewBag.Doctors = new SelectList(doctors, "Id", "Name", selectedDoctorId);
        }
    }
}
using HospitalMS.BLL.ModelVM.DoctorSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalMS.PL.Controllers.Doctor
{
    [Authorize]
    public class DoctorScheduleController : Controller
    {
        private const int MaxPageSize = 50;

        private readonly IDoctorScheduleService _doctorScheduleService;
        private readonly IDoctorService _doctorService;

        public DoctorScheduleController(
            IDoctorScheduleService doctorScheduleService,
            IDoctorService doctorService)
        {
            _doctorScheduleService = doctorScheduleService;
            _doctorService = doctorService;
        }

        private string CurrentUser => User.Identity?.Name ?? "System";

        [HttpGet]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Index(
            string? search, int page = 1, int pageSize = 7, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 7;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var pagedResult = await _doctorScheduleService.GetPagedAsync(search, page, pageSize, ct);
            return View(pagedResult);
        }

        [HttpGet]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var schedule = await _doctorScheduleService.GetDetailsAsync(id, ct);

            if (schedule is null)
                return NotFound();

            return View(schedule);
        }

        [HttpGet]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Create(Guid? doctorId, CancellationToken ct)
        {
            var model = new NewDoctorScheduleViewModel();

            if (doctorId.HasValue)
            {
                var doctor = await _doctorService.GetByIdAsync(doctorId.Value, ct);
                if (doctor is null)
                    return NotFound();

                model.DoctorId = doctor.Id;
                model.DoctorName = doctor.Name;
            }

            await LoadDropdowns(ct, model.DoctorId == Guid.Empty ? null : model.DoctorId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Create(NewDoctorScheduleViewModel doctorScheduleVm, CancellationToken ct)
        {
            if (doctorScheduleVm.DoctorId == Guid.Empty)
            {
                ModelState.Remove(nameof(doctorScheduleVm.DoctorId));
                ModelState.AddModelError(nameof(doctorScheduleVm.DoctorId), "Doctor is required.");
            }

            if (doctorScheduleVm.StartTime == default)
            {
                ModelState.Remove(nameof(doctorScheduleVm.StartTime));
                ModelState.AddModelError(nameof(doctorScheduleVm.StartTime), "Start time is required.");
            }

            if (doctorScheduleVm.EndTime == default)
            {
                ModelState.Remove(nameof(doctorScheduleVm.EndTime));
                ModelState.AddModelError(nameof(doctorScheduleVm.EndTime), "End time is required.");
            }

            if (doctorScheduleVm.Days == null || !doctorScheduleVm.Days.Any())
            {
                ModelState.AddModelError(nameof(doctorScheduleVm.Days), "At least one day is required.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ct, doctorScheduleVm.DoctorId);
                return View(doctorScheduleVm);
            }

            var result = await _doctorScheduleService.CreateAsync(doctorScheduleVm, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    AddScheduleError(
                        error,
                        nameof(doctorScheduleVm.StartTime),
                        nameof(doctorScheduleVm.EndTime),
                        nameof(doctorScheduleVm.DoctorId),
                        nameof(doctorScheduleVm.Days));
                }

                await LoadDropdowns(ct, doctorScheduleVm.DoctorId);
                return View(doctorScheduleVm);
            }

            TempData["Success"] = "Doctor schedule created successfully.";
            return RedirectToAction("Details", "Doctor", new { id = doctorScheduleVm.DoctorId });
        }

        [HttpGet]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var schedule = await _doctorScheduleService.GetByIdAsync(id, ct);

            if (schedule is null)
                return NotFound();

            await LoadDropdowns(ct, schedule.DoctorId);
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Edit(UpdateDoctorScheduleViewModel updateDoctorSchedule, CancellationToken ct)
        {
            if (updateDoctorSchedule.DoctorId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(updateDoctorSchedule.DoctorId), "Doctor is required.");
            }

            if (updateDoctorSchedule.Days == null || !updateDoctorSchedule.Days.Any())
            {
                ModelState.AddModelError(nameof(updateDoctorSchedule.Days), "At least one day is required.");
            }

            if (updateDoctorSchedule.StartTime == default)
            {
                ModelState.Remove(nameof(updateDoctorSchedule.StartTime));
                ModelState.AddModelError(nameof(updateDoctorSchedule.StartTime), "Start time is required.");
            }

            if (updateDoctorSchedule.EndTime == default)
            {
                ModelState.Remove(nameof(updateDoctorSchedule.EndTime));
                ModelState.AddModelError(nameof(updateDoctorSchedule.EndTime), "End time is required.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ct, updateDoctorSchedule.DoctorId);
                return View(updateDoctorSchedule);
            }

            var result = await _doctorScheduleService.UpdateAsync(updateDoctorSchedule, CurrentUser, ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    AddScheduleError(
                        error,
                        nameof(updateDoctorSchedule.StartTime),
                        nameof(updateDoctorSchedule.EndTime),
                        nameof(updateDoctorSchedule.DoctorId),
                        nameof(updateDoctorSchedule.Days));
                }

                await LoadDropdowns(ct, updateDoctorSchedule.DoctorId);
                return View(updateDoctorSchedule);
            }

            TempData["Success"] = "Doctor schedule updated successfully.";
            return RedirectToAction("Details", "Doctor", new { id = updateDoctorSchedule.DoctorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Doctors.Schedule)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _doctorScheduleService.DeleteAsync(id, CurrentUser, ct);

            if (result.Succeeded)
                TempData["Success"] = "Doctor schedule deleted successfully.";
            else
                TempData["Error"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private void AddScheduleError(
            string error, string startKey, string endKey, string doctorKey, string daysKey)
        {
            if (error.Contains("Start time", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(startKey, error);
            else if (error.Contains("End time", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(endKey, error);
            else if (error.Contains("Doctor", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(doctorKey, error);
            else if (error.Contains("day", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(daysKey, error);
            else
                ModelState.AddModelError(string.Empty, error);
        }

        private async Task LoadDropdowns(CancellationToken ct, Guid? selectedDoctorId = null)
        {
            var doctors = await _doctorService.GetAllAsync(ct);
            ViewBag.Doctors = new SelectList(doctors, "Id", "Name", selectedDoctorId);
        }
    }
}
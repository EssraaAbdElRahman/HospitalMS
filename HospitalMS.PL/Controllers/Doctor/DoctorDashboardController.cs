using HospitalMS.BLL.ModelVM.DoctorDashboard;
using HospitalMS.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalMS.PL.Controllers
{
    [Authorize]
    public class DoctorDashboardController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;

        public DoctorDashboardController(
            IDoctorService doctorService,
            IAppointmentService appointmentService)
        {
            _doctorService = doctorService;
            _appointmentService = appointmentService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? date, AppointmentStatus? status, string? searchTerm, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var doctorId = await _doctorService.GetByUserIdAsync(userId, ct);
            if (!doctorId.HasValue)
                return Forbid();

            var doctor = await _doctorService.GetDetailsAsync(doctorId.Value, ct);
            if (doctor is null)
                return NotFound();

            var selectedDate = date?.Date ?? DateTime.Today;

            var result = await _appointmentService.GetFilteredAsync(
                selectedDate, doctorId.Value, status, searchTerm, ct);

            var model = new DoctorDashboardViewModel
            {
                DoctorName = doctor.Name,
                DepartmentName = doctor.DepartmentName,
                SelectedDate = selectedDate,
                Status = status,
                SearchTerm = searchTerm,
                TotalToday = result.TotalItems
            };

            foreach (var appointment in result.Items)
            {
                model.Queue.Add(new DoctorDashboardPatientViewModel
                {
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = appointment.PatientName,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentType = appointment.AppointmentType,
                    AppointmentStatus = appointment.Status
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(Guid id, DateTime? date, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var doctorId = await _doctorService.GetByUserIdAsync(userId, ct);
            if (!doctorId.HasValue)
                return Forbid();

            var appointment = await _appointmentService.GetByIdAsync(id, ct);

            if (appointment is null || appointment.DoctorId != doctorId.Value)
                return Forbid();

            if (appointment.Status != AppointmentStatus.Check_in)
            {
                TempData["Error"] = "Only checked-in appointments can be completed.";
                return RedirectToAction(nameof(Index), new { date });
            }

            var result = await _appointmentService.UpdateStatusAsync(
                id, AppointmentStatus.Completed, userId, ct);

            if (result.Succeeded)
                TempData["Success"] = "Visit completed.";
            else
                TempData["Error"] = string.Join(" ", result.Errors);

            return RedirectToAction(nameof(Index), new { date });
        }
    }
}
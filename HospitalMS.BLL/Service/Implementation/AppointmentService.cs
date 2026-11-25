using HospitalMS.BLL.ModelVM.Appointment;
using HospitalMS.DAL.Models.AppointmentModule;

namespace HospitalMS.BLL.Service.Implementation
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<AppointmentListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Appointments.GetAllAsync(ct);
            var active = items.Where(x => !x.IsDeleted).ToList();
            return await MapToListItemsAsync(active, ct);
        }

        public async Task<List<AppointmentListItemViewModel>> GetByDateAsync(DateTime date, CancellationToken ct)
        {
            var items = await _unitOfWork.Appointments.GetByDateAsync(date, ct);
            return await MapToListItemsAsync(items, ct);
        }

        public async Task<List<AppointmentListItemViewModel>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
        {
            var items = await _unitOfWork.Appointments.GetByPatientIdAsync(patientId, ct);
            var active = items.Where(x => !x.IsDeleted).ToList();
            return await MapToListItemsAsync(active, ct);
        }

        public async Task<UpdateAppointmentViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, ct);
            if (appointment is null || appointment.IsDeleted)
                return null;

            return await BuildUpdateViewModelAsync(appointment, ct);
        }

        public async Task<ServiceResult<Guid>> CreateAsync(
            NewAppointmentViewModel vm, string createdBy, CancellationToken ct)
        {
            var appointmentDateTime = vm.AppointmentDate.Date.Add(vm.AppointmentTime);

            if (await HasConflictAsync(vm.DoctorId, appointmentDateTime, null, ct))
                return ServiceResult<Guid>.Failure("This doctor already has an appointment at the selected time.");

            try
            {
                var appointment = new Appointment(
                    vm.PatientId, vm.DoctorId, appointmentDateTime, vm.AppointmentType);

                await _unitOfWork.Appointments.AddAsync(appointment, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(appointment.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateAsync(
            UpdateAppointmentViewModel vm, string updatedBy, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(vm.Id, ct);
            if (appointment is null || appointment.IsDeleted)
                return ServiceResult.Failure("Appointment not found.");

            var newDateTime = vm.AppointmentDate.Date.Add(vm.AppointmentTime);

            if (await HasConflictAsync(vm.DoctorId, newDateTime, vm.Id, ct))
                return ServiceResult.Failure("This doctor already has an appointment at the selected time.");

            try
            {
                await _unitOfWork.Appointments.RescheduleAsync(vm.Id, newDateTime, updatedBy, ct);
                await _unitOfWork.Appointments.UpdateStatusAsync(vm.Id, vm.Status, updatedBy, ct);
                await _unitOfWork.Appointments.ChangeDoctorAsync(vm.Id, vm.DoctorId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> ChangeDoctorAsync(
            Guid id, Guid doctorId, string updatedBy, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, ct);
            if (appointment is null || appointment.IsDeleted)
                return ServiceResult.Failure("Appointment not found.");

            if (await HasConflictAsync(doctorId, appointment.AppointmentDate, id, ct))
                return ServiceResult.Failure("This doctor already has an appointment at the selected time.");

            try
            {
                await _unitOfWork.Appointments.ChangeDoctorAsync(id, doctorId, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateStatusAsync(
            Guid id, AppointmentStatus status, string updatedBy, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, ct);
            if (appointment is null || appointment.IsDeleted)
                return ServiceResult.Failure("Appointment not found.");

            try
            {
                await _unitOfWork.Appointments.UpdateStatusAsync(id, status, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> RescheduleAsync(
            Guid id, DateTime appointmentDate, string updatedBy, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, ct);
            if (appointment is null || appointment.IsDeleted)
                return ServiceResult.Failure("Appointment not found.");

            if (await HasConflictAsync(appointment.DoctorId, appointmentDate, id, ct))
                return ServiceResult.Failure("This doctor already has an appointment at the selected time.");

            try
            {
                await _unitOfWork.Appointments.RescheduleAsync(id, appointmentDate, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, ct);
            if (appointment is null || appointment.IsDeleted)
                return ServiceResult.Failure("Appointment not found.");

            try
            {
                await _unitOfWork.Appointments.DeleteAsync(id, deletedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<AppointmentFilterResult> GetFilteredAsync(
            DateTime date, Guid? doctorId, AppointmentStatus? status, string? searchTerm, CancellationToken ct)
        {
            var appointments = await GetByDateAsync(date.Date, ct);

            if (doctorId.HasValue && doctorId != Guid.Empty)
                appointments = appointments.Where(a => a.DoctorId == doctorId.Value).ToList();

            if (status.HasValue)
                appointments = appointments.Where(a => a.Status == status.Value).ToList();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                appointments = appointments
                    .Where(a => a.PatientName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return new AppointmentFilterResult
            {
                Items = appointments.OrderBy(a => a.AppointmentDate).ToList(),
                TotalItems = appointments.Count,
                ScheduledCount = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                CheckedInCount = appointments.Count(a => a.Status == AppointmentStatus.Check_in),
                CompletedCount = appointments.Count(a => a.Status == AppointmentStatus.Completed)
            };
        }

        public Task<ServiceResult> NoShowAsync(Guid appointmentId, string performedBy, CancellationToken ct)
            => UpdateStatusAsync(appointmentId, AppointmentStatus.No_show, performedBy, ct);

        public Task<ServiceResult> CancelAsync(Guid appointmentId, string performedBy, CancellationToken ct)
            => UpdateStatusAsync(appointmentId, AppointmentStatus.Canceled, performedBy, ct);

        // ---------- Helpers ----------

        private async Task<bool> HasConflictAsync(
            Guid doctorId, DateTime appointmentDateTime, Guid? excludeId, CancellationToken ct)
        {
            var sameDay = await _unitOfWork.Appointments.GetByDateAsync(appointmentDateTime.Date, ct);

            return sameDay.Any(a =>
                !a.IsDeleted &&
                a.DoctorId == doctorId &&
                a.AppointmentDate == appointmentDateTime &&
                (excludeId == null || a.Id != excludeId));
        }

        private async Task<List<AppointmentListItemViewModel>> MapToListItemsAsync(
            List<Appointment> appointments, CancellationToken ct)
        {
            if (appointments.Count == 0)
                return new List<AppointmentListItemViewModel>();

            var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
            var doctorIds = appointments.Select(a => a.DoctorId).Distinct().ToList();

            var patientNames = await _unitOfWork.Patients.GetNamesByIdsAsync(patientIds, ct);
            var doctorNames = await _unitOfWork.Doctors.GetNamesByIdsAsync(doctorIds, ct);

            return appointments.Select(x => new AppointmentListItemViewModel
            {
                Id = x.Id,
                PatientId = x.PatientId,
                PatientName = patientNames.GetValueOrDefault(x.PatientId, "Unknown"),
                DoctorId = x.DoctorId,
                DoctorName = doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"),
                AppointmentDate = x.AppointmentDate,
                Status = x.Status,
                AppointmentType = x.AppointmentType,
                CreatedAt = x.CreatedAt,
                IsDeleted = x.IsDeleted
            }).ToList();
        }

        private async Task<UpdateAppointmentViewModel> BuildUpdateViewModelAsync(
            Appointment appointment, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(appointment.PatientId, ct);
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId, ct);

            return new UpdateAppointmentViewModel
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                AppointmentType = appointment.AppointmentType,
                AppointmentTime = appointment.AppointmentDate.TimeOfDay,
                DepartmentId = doctor is null ? Guid.Empty : doctor.DepartmentId,
                PatientName = patient?.Name ?? "Unknown"
            };
        }
    }
}
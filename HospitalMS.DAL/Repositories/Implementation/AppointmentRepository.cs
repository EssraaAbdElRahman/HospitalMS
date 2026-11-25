using HospitalMS.DAL.Models.AppointmentModule;

namespace HospitalMS.DAL.Repositories.Implementation
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HospitalDbContext _context;

        public AppointmentRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Appointment entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Appointments.AddAsync(entity, ct);
        }

        public async Task ChangeDoctorAsync(Guid appointmentId, Guid doctorId, string updatedBy, CancellationToken ct)
        {
            var appointment = await GetByIdWithTrackingAsync(appointmentId, ct);
            appointment.ChangeDoctor(doctorId);
            appointment.Updated(updatedBy);
        }

        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var appointment = await GetByIdWithTrackingAsync(id, ct);
            appointment.Deleted(deletedBy);
        }

        public async Task<List<Appointment>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Appointments.ToListAsync(ct);
        }
        public async Task<List<Appointment>> GetByDateAsync(DateTime date, CancellationToken ct)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Where(x => x.AppointmentDate >= start && x.AppointmentDate < end)
                .OrderBy(x => x.AppointmentDate)
                .ToListAsync(ct);
        }

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task RescheduleAsync(Guid id, DateTime appointmentDate, string updatedBy, CancellationToken ct)
        {
            var appointment = await GetByIdWithTrackingAsync(id, ct);

            appointment.Reschedule(appointmentDate);
            appointment.Updated(updatedBy);
        }

        public async Task UpdateAsync(Appointment entity, string updatedBy, CancellationToken ct)
        {
            throw new NotSupportedException("Use UpdateStatus or Reschedule instead.");
        }

        public async Task UpdateStatusAsync(Guid appointmentId, AppointmentStatus status, string updatedBy, CancellationToken ct)
        {
            var appointment = await GetByIdWithTrackingAsync(appointmentId, ct);

            appointment.UpdateStatus(status);
            appointment.Updated(updatedBy);
        }
        private async Task<Appointment> GetByIdWithTrackingAsync(Guid id, CancellationToken ct)
        {
            return await _context.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException($"Appointment with Id {id} was not found.");
        }
        public async Task<List<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId == patientId && !x.IsDeleted)
                .OrderByDescending(x => x.AppointmentDate)
                .ToListAsync(ct);
        }

    }
}

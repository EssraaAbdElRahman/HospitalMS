using HospitalMS.DAL.Models.AppointmentModule;
namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task RescheduleAsync(Guid id, DateTime appointmentDate, string updatedBy, CancellationToken ct);
        Task UpdateStatusAsync(Guid appointmentId, AppointmentStatus status, string updatedBy, CancellationToken ct);
        Task ChangeDoctorAsync(Guid appointmentId, Guid doctorId, string updatedBy, CancellationToken ct);
        Task<List<Appointment>> GetByDateAsync(DateTime date, CancellationToken ct);
        Task<List<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    }
}
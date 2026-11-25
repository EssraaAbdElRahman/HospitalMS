using HospitalMS.BLL.ModelVM.Appointment;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IAppointmentService
    {
        Task<List<AppointmentListItemViewModel>> GetAllAsync(CancellationToken ct);
        Task<List<AppointmentListItemViewModel>> GetByDateAsync(DateTime date, CancellationToken ct);
        Task<UpdateAppointmentViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewAppointmentViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
        Task<ServiceResult> UpdateStatusAsync(Guid id, AppointmentStatus status, string updatedBy, CancellationToken ct);
        Task<ServiceResult> RescheduleAsync(Guid id, DateTime appointmentDate, string updatedBy, CancellationToken ct);
        Task<List<AppointmentListItemViewModel>> GetByPatientIdAsync(Guid patientId, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateAppointmentViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> ChangeDoctorAsync(Guid id, Guid doctorId, string updatedBy, CancellationToken ct);
        Task<ServiceResult> NoShowAsync(Guid appointmentId, string performedBy, CancellationToken ct);
        Task<ServiceResult> CancelAsync(Guid appointmentId, string performedBy, CancellationToken ct);
        Task<AppointmentFilterResult> GetFilteredAsync(DateTime date, Guid? doctorId, AppointmentStatus? status, string? searchTerm, CancellationToken ct);
    }
}
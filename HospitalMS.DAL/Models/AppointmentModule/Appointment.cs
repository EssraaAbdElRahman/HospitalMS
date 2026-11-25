using HospitalMS.DAL.Models.PatientModule;
namespace HospitalMS.DAL.Models.AppointmentModule
{
    public class Appointment : BaseEntity
    {
        public Guid PatientId { get; private set; }
        public Patient Patient { get; private set; } = null!;

        public Guid DoctorId { get; private set; }
        public Doctor Doctor { get; private set; } = null!;

        public DateTime AppointmentDate { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public AppointmentType AppointmentType { get; private set; }
        private Appointment() { }
     
        public Appointment(Guid patientId, Guid doctorId, DateTime appointmentDate, AppointmentType appointmentType)
        {
            if (patientId == Guid.Empty)
                throw new ArgumentException("PatientId is required");

            if (doctorId == Guid.Empty)
                throw new ArgumentException("DoctorId is required");

            PatientId = patientId;
            DoctorId = doctorId;
            AppointmentDate = appointmentDate;
            AppointmentType = appointmentType;
            Status = AppointmentStatus.Scheduled;
        }
        public void ChangeDoctor(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
                throw new ArgumentException("DoctorId is required");

            DoctorId = doctorId;
        }
        public void UpdateStatus(AppointmentStatus status)
        {
            Status = status;
        }

        public void Reschedule(DateTime appointmentDate)
        {
            if (appointmentDate <= DateTime.Now)
                throw new ArgumentException("Appointment date must be in the future");

            AppointmentDate = appointmentDate;
        }
    }
}

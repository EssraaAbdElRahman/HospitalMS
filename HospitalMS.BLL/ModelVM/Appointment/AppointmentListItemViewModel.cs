
namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class AppointmentListItemViewModel
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = null!; 
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = null!; 
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public AppointmentType AppointmentType { get; set; }
         public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}

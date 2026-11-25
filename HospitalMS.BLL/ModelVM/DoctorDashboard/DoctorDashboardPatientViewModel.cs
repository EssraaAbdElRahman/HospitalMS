namespace HospitalMS.BLL.ModelVM.DoctorDashboard
{
    public class DoctorDashboardPatientViewModel
    {
        public Guid AppointmentId { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public DateTime AppointmentDate { get; set; }

        public AppointmentType AppointmentType { get; set; }

        public AppointmentStatus AppointmentStatus { get; set; }

        public Guid? EncounterId { get; set; }

        public Guid? VisitId { get; set; }

        public DateTime? VisitEndTime { get; set; }

        public string Status { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;


    }
}
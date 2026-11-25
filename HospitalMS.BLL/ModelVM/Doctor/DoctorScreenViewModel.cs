namespace HospitalMS.BLL.ModelVM.Doctor
{
    public class DoctorScreenViewModel
    {
        public Guid TicketId { get; set; }
        public Guid VisitId { get; set; }
        public Guid MedicalRecordId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
    }
}
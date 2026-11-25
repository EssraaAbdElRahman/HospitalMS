namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class QuickAddPatientViewModel
    {
        public string PatientName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public Gender Gender { get; set; }
        public string Address { get; set; } = "";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public string? Notes { get; set; }
    }
}

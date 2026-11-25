using HospitalMS.DAL.Models.AppModule;

namespace HospitalMS.BLL.ModelVM.Patient
{
    public class PatientDetailsViewModel
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Notes { get; set; } = string.Empty;

    }
}
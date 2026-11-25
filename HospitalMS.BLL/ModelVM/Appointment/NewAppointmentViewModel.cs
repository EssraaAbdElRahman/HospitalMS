namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class NewAppointmentViewModel
    {
        [Required(ErrorMessage = "Patient is required.")]
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        public AppointmentType AppointmentType { get; set; }
        public Guid? EncounterId { get; set; }

        [Required(ErrorMessage = "Department is required ")]
        public Guid DepartmentId { get; set; }

        [Required(ErrorMessage = " Doctor is required  ")]
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Please select an appointment date.")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select an available time slot.")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        public string? Notes { get; set; }
    }
}

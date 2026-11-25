namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class UpdateAppointmentViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Patient is required.")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "Please select a doctor.")]
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Please select the appointment date.")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a department.")]
        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        [Required(ErrorMessage = "Please select the appointment status.")]
        public AppointmentStatus Status { get; set; }

        [Required(ErrorMessage = "Please select the appointment type.")]
        public AppointmentType AppointmentType { get; set; }

        [Required(ErrorMessage = "Please select a time slot.")]
        public TimeSpan AppointmentTime { get; set; }

        public Guid? EncounterId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string DoctorName { get; set; } = string.Empty;
       public  string? ReturnUrl { get; set; }
    }
}
namespace HospitalMS.BLL.ModelVM.DoctorSchedule
{
    public class UpdateDoctorScheduleViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public Guid DoctorId { get; set; }
        public List<Days> Days { get; set; } = new();

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }
    }
}

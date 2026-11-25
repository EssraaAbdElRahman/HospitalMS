
namespace HospitalMS.BLL.ModelVM.DoctorSchedule
{
    public class NewDoctorScheduleViewModel
    {
        [Required(ErrorMessage = "At least one day is required.")]
        public List<Days> Days { get; set; } = new();

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        [Required]
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
    }
}

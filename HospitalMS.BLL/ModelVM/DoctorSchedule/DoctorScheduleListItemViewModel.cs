
namespace HospitalMS.BLL.ModelVM.DoctorSchedule
{
    public class DoctorScheduleListItemViewModel
    {
        public Guid Id { get; set; }
        public string Days { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}

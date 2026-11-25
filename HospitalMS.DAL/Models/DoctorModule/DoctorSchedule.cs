namespace HospitalMS.DAL.Models.DoctorModule
{
    public class DoctorSchedule : BaseEntity
    {
        public string Days { get; private set; } = string.Empty;
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public Guid DoctorId { get; private set; }
        public Doctor Doctor { get; private set; } = null!;
        private DoctorSchedule() { }

        public DoctorSchedule(string days, DateTime startTime, DateTime endTime, Guid doctorId)
        {
            ValidateDays(days);
            SetDoctorSchedule(days, doctorId, startTime, endTime);
        }
        public void UpdateInfo(string days, Guid doctorId, DateTime startTime, DateTime endTime)
        {
            ValidateDays(days);
            SetDoctorSchedule(days, doctorId, startTime, endTime);
        }
        private void ValidateDays(string days)
        {
            if (string.IsNullOrWhiteSpace(days))
                throw new ArgumentException("At least one day is required.");
        }
        private void SetDoctorSchedule(string days, Guid doctorId, DateTime startTime, DateTime endTime)
        {
            Days = days;
            SetDoctorId(doctorId);
            SetStartEndTime(startTime, endTime);
        }
        private void SetStartEndTime(DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime)
                throw new ArgumentException("End time must be after start time");

            StartTime = startTime;
            EndTime = endTime;
        }
        private void SetDoctorId(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
                throw new ArgumentException("Doctor is required");
            DoctorId = doctorId;
        }
    }
}
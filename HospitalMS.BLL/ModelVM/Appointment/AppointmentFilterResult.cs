namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class AppointmentFilterResult
    {
        public List<AppointmentListItemViewModel> Items { get; set; } = new();

        // Counts calculated from the FULL filtered set (before paging)
        public int TotalItems { get; set; }
        public int ScheduledCount { get; set; }
        public int CheckedInCount { get; set; }
        public int CompletedCount { get; set; }

    }
}
namespace HospitalMS.BLL.ModelVM.Appointment
{
    public class AppointmentIndexViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<AppointmentListItemViewModel> Appointments { get; set; } = new();

        // Counts are now calculated in the Controller from the FILTERED (but not paged) set,
        // so they reflect whatever filter is currently applied.
        public int TotalCount { get; set; }
        public int ScheduledCount { get; set; }
        public int CheckedInCount { get; set; }
        public int CompletedCount { get; set; }

        // ===== Filters (kept here so the View can re-populate the form and build pagination links) =====
        public Guid? DoctorId { get; set; }
        public AppointmentStatus? Status { get; set; }
        public string? SearchTerm { get; set; }

        // ===== Pagination =====
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
    }
}

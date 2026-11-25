namespace HospitalMS.BLL.ModelVM.DoctorDashboard
{
    public class DoctorDashboardViewModel
    {
        public string DoctorName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public DateTime SelectedDate { get; set; }

        public int TotalToday { get; set; }

        public int WaitingCount { get; set; }

        public int InVisitCount { get; set; }

        public int CompletedCount { get; set; }

        public AppointmentStatus? Status { get; set; }
        public string? SearchTerm { get; set; }

        public List<DoctorDashboardPatientViewModel> Queue { get; set; } = new();
    }
}
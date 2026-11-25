namespace HospitalMS.BLL.ModelVM.UserAccount
{
    public class UsersSummaryViewModel
    {
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int StaffCount { get; set; }
        public int PatientCount { get; set; }
        public int DoctorCount { get; set; }
        public int ReceptionistCount { get; set; }
        public int TotalActive { get; set; }
        public int TotalUserPending { get; set; }
        public int TotalPasswordResetRequests { get; set; }
    }
}

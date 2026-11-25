namespace HospitalMS.BLL.ModelVM.Patient
{
    public class PatientSearchResultViewModel
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
    public class PatientSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public List<PatientSearchResultViewModel> Results { get; set; } = new();
    }

}

namespace HospitalMS.BLL.ModelVM.Doctor
{
    public class DoctorListItemViewModel
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public Guid SpecialtyId { get; set; }
        public int YearsOfExperience { get; set; }
        public Guid DepartmentId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
    }
}

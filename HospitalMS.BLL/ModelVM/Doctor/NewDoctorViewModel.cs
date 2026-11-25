
namespace HospitalMS.BLL.ModelVM.Doctor
{
    public class NewDoctorViewModel
    {
        [Required(ErrorMessage = "Doctor name is required.")]
        [StringLength(100, ErrorMessage = "Doctor name cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z]+(\s[a-zA-Z]+)*$", ErrorMessage = "Doctor Name can contain letters and single spaces between words only.")]
        public string Name { get; set; } = string.Empty;
        public string? UserId { get; set; }

        [Required(ErrorMessage = "National ID is required.")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain digits only.")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bio is required.")]
        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
        public string Bio { get; set; } = string.Empty;


        [Required(ErrorMessage = "Specialty is required.")]
        public Guid? SpecialtyId { get; set; }


        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0,int.MaxValue, ErrorMessage = "Years of experience cannot be negative.")]
        public int? YearsOfExperience { get; set; }


        [Required(ErrorMessage = "Department is required.")]
        public Guid? DepartmentId { get; set; }

        [Required(ErrorMessage = "Hire date is required.")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Required(ErrorMessage = "Salary is required.")]
        [Range(1000, 1000000, ErrorMessage = "Salary must be between 1,000 and 1,000,000.")]
        public decimal? Salary { get; set; }
    }
}

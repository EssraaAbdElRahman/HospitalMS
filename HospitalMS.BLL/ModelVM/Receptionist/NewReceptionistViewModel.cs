
namespace HospitalMS.BLL.ModelVM.Receptionist
{
    public class NewReceptionistViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "National ID is required.")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain digits only.")]
        public string NationalId { get; set; } = string.Empty;
        [DataType(DataType.DateTime)]
        public DateTime HireDate { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal Salary { get; set; }
    }
}

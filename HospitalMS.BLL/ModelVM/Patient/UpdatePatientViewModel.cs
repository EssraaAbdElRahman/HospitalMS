namespace HospitalMS.BLL.ModelVM.Patient
{
    public class UpdatePatientViewModel
    {
        public Guid Id { get; set; }
       
        [Required]
        [StringLength(100)]
        public string PatientName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
      
        [Required]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;
       
        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;
       
        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;
        public string? Notes { get; set; }

        [Required(ErrorMessage = "National ID is required.")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain digits only.")]
        public string NationalId { get; set; } = string.Empty;
    }
}

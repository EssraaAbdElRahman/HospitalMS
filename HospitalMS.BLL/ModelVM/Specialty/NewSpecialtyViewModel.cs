
namespace HospitalMS.BLL.ModelVM.Specialty
{
    public class NewSpecialtyViewModel
    {
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z]+(\s[a-zA-Z]+)*$", ErrorMessage = "Specialty Name can contain letters and single spaces between words only.")]
        public string Name { get; set; } = string.Empty;
    }
}


namespace HospitalMS.BLL.ModelVM.Department
{
    public class UpdateDepartmentViewModel
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z]+(\s[a-zA-Z]+)*$", ErrorMessage = "Department Name can contain letters and single spaces between words only.")]
        public string Name { get; set; } = string.Empty;
    }
}

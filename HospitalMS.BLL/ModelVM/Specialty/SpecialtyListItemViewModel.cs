namespace HospitalMS.BLL.ModelVM.Specialty
{
    public class SpecialtyListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}

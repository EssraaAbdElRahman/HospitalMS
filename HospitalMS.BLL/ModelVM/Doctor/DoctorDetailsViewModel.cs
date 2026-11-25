using HospitalMS.BLL.ModelVM.DoctorSchedule;

namespace HospitalMS.BLL.ModelVM.Doctor
{
    public class DoctorDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DoctorScheduleListItemViewModel> Schedules { get; set; } = [];
    }
}

using HospitalMS.DAL.Common.Validators;
using HospitalMS.DAL.Models.AppModule;
using HospitalMS.DAL.Models.AppointmentModule;
namespace HospitalMS.DAL.Models.DoctorModule
{
    public class Doctor : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;

        public string? UserId { get; private set; }
        public ApplicationUser? User { get; private set; }
        public string Bio { get; private set; } = string.Empty;
        public Guid SpecialtyId { get; private set; }
        public Specialty Specialty { get; private set; } = null!;

        public int YearsOfExperience { get; private set; }

        public Guid DepartmentId { get; private set; }
        public Department Department { get; private set; } = null!;

        public string NationalId { get; private set; } = string.Empty;
        public DateTime HireDate { get; private set; }
        public decimal Salary { get; private set; }
        private readonly List<DoctorSchedule> _doctorSchedules = [];
        public IReadOnlyCollection<DoctorSchedule> DoctorSchedules => _doctorSchedules.AsReadOnly();
        private readonly List<Appointment> _appointment = [];
        public IReadOnlyCollection<Appointment> Appointments
            => _appointment.AsReadOnly();

        private Doctor() { }

        public Doctor(string name, string? userId, string nationalId, string bio, Guid specialtyId, int yearsOfExperience, Guid departmentId, DateTime hireDate, decimal salary)
        {
            UserId = userId;
            SetDoctorData(name, nationalId, bio, specialtyId,
                yearsOfExperience, departmentId, hireDate, salary);
        }

        public void UpdateInfo(string name, string nationalId, string bio, Guid specialtyId, int yearsOfExperience, Guid departmentId, DateTime hireDate, decimal salary)
        {
            SetDoctorData(name, nationalId, bio, specialtyId,
                yearsOfExperience, departmentId, hireDate, salary);
        }
        private void SetDoctorData(string name, string nationalId, string bio, Guid specialtyId, int yearsOfExperience, Guid departmentId, DateTime hireDate, decimal salary)
        {
            SetName(name);
            SetBio(bio);
            SetNationalId(nationalId);
            SetYearOfExperience(yearsOfExperience);
            SetSpecialtyId(specialtyId);
            SetDepartmentId(departmentId);
            SetHireDate(hireDate);
            SetSalary(salary);
        }

        private void SetName(string name)
        {
            Name = PersonNameValidator.Validate(name);
        }
        private void SetNationalId(string nationalId)
        {
            NationalId = NationalIdValidator.Validate(nationalId);
        }
        private void SetBio(string bio)
        {
            if (string.IsNullOrWhiteSpace(bio))
                throw new ArgumentException("Bio is required");
            Bio = bio.Trim();
        }

        private void SetYearOfExperience(int yearsOfExperience)
        {
            if (yearsOfExperience < 0)
                throw new ArgumentException("Years of experience cannot be negative");
            YearsOfExperience = yearsOfExperience;
        }

        public void AssignUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required");

            if (!string.IsNullOrEmpty(UserId))
                throw new InvalidOperationException("This doctor already has a linked user account.");

            UserId = userId;
        }
        public void ChangeSalary(decimal salary)
        {
            SetSalary(salary);
        }
        private void SetDepartmentId(Guid departmentId)
        {
            if (departmentId == Guid.Empty)
                throw new ArgumentException("Department is required");
            DepartmentId = departmentId;
        }

        private void SetHireDate(DateTime hireDate)
        {
            if (hireDate > DateTime.UtcNow)
                throw new ArgumentException("Hire date cannot be in the future");
            HireDate = hireDate;
        }

        private void SetSalary(decimal salary)
        {
            if (salary <= 0)
                throw new ArgumentException("Salary must be positive");
            Salary = salary;
        }
        public void ChangeDepartment(Guid departmentId)
        {
            SetDepartmentId(departmentId);
        }
        public void ChangeSpecialty(Guid specialtyId)
        {
            SetSpecialtyId(specialtyId);

        }

        private void SetSpecialtyId(Guid specialtyId)
        {
            if (specialtyId == Guid.Empty)
                throw new ArgumentException("Specialty is required");
            SpecialtyId = specialtyId;
        }
    }
}
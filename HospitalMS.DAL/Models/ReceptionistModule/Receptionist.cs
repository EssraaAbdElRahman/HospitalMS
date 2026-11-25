using HospitalMS.DAL.Common.Validators;
using HospitalMS.DAL.Models.AppModule;

namespace HospitalMS.DAL.Models.ReceptionistModule
{
    public class Receptionist : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string? UserId { get; private set; }
        public ApplicationUser? User { get; }
        public string NationalId { get; private set; } = string.Empty;
        public DateTime HireDate { get; private set; }
        public decimal Salary { get; private set; }

        private Receptionist() { }

        public Receptionist(string name, string nationalId, DateTime hireDate, decimal salary)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length != 14)
                throw new ArgumentException("Invalid National ID");

            if (salary <= 0)
                throw new ArgumentException("Salary must be positive");

            if (hireDate > DateTime.Now)
                throw new ArgumentException("Hire date cannot be in the future");

            Name = name;
            NationalId = nationalId;
            HireDate = hireDate;
            Salary = salary;

        }

        public void UpdateInfo(string name, string nationalId, decimal salary)
        {
            SetReceptionistData(name, nationalId, salary);
        }
        private void SetReceptionistData(string name, string nationalId, decimal salary)
        {
            SetName(name);
            SetSalary(salary);
            SetNationalId(nationalId);
        }
        private void SetName(string name)
        {
            Name = PersonNameValidator.Validate(name);
        }
        private void SetNationalId(string nationalId)
        {
            NationalId = NationalIdValidator.Validate(nationalId);
        }
        public void ChangeSalary(decimal salary)
        {
            SetSalary(salary);
        }
        private void SetSalary(decimal salary)
        {
            if (salary <= 0)
                throw new ArgumentException("Salary must be positive");
            Salary = salary;
        }
        public void AssignUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required");

            if (!string.IsNullOrEmpty(UserId))
                throw new InvalidOperationException("This receptionist already has a linked user account.");

            UserId = userId;
        }
    }
}

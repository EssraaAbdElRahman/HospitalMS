using HospitalMS.DAL.Common.Validators;
using HospitalMS.DAL.Models.AppModule;
using HospitalMS.DAL.Models.AppointmentModule;
namespace HospitalMS.DAL.Models.PatientModule
{
    public class Patient : BaseEntity
    {
        public string? UserId { get; private set; }
        public ApplicationUser? User { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public string NationalId { get; private set; } = string.Empty;
        public Gender Gender { get; private set; }
        public string Address { get; private set; } = string.Empty;
        public string Country { get; private set; } = string.Empty;
        public string City { get; private set; } = string.Empty;
        public string? Notes { get; private set; }
        private readonly List<Appointment> _appointment = [];
        public IReadOnlyCollection<Appointment> Appointments => _appointment.AsReadOnly();


        private Patient() { }

        public Patient(string patientName, string nationalId, Gender gender, string address, string country, string city, string? notes)
        {
            SetPatientData(patientName, gender, address, country, city, notes);
            SetNationalId(nationalId);
        }
        public void UpdateInfo(string patientName, Gender gender, string address, string country, string city, string? notes)
        {
            SetPatientData(patientName, gender, address, country, city, notes);
        }
        private void SetPatientData(string patientName, Gender gender, string address,
            string country, string city, string? notes)
        {
            SetName(patientName);
            SetGender(gender);
            SetAddress(address);
            SetCountry(country);
            SetCity(city);
            SetNotes(notes);
        }
        private void SetNotes(string? notes)
        {
            Notes = notes;
        }
        private void SetCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City is required");
            City = city.Trim();
        }
        private void SetCountry(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country is required");
            Country = country.Trim();
        }

        private void SetAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is required");
            Address = address.Trim();
        }

        private void SetGender(Gender gender)
        {
            if (!Enum.IsDefined(gender))
                throw new ArgumentException("Invalid gender.");

            Gender = gender;
        }
        private void SetName(string name)
        {
            Name = PersonNameValidator.Validate(name);
        }
        private void SetNationalId(string nationalId)
        {
            NationalId = NationalIdValidator.Validate(nationalId);
        }
        public void AssignUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required");

            if (!string.IsNullOrEmpty(UserId))
                throw new InvalidOperationException("This patient  already has a linked user account.");

            UserId = userId;
        }
    }
}

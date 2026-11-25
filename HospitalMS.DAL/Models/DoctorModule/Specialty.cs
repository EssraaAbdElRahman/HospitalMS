namespace HospitalMS.DAL.Models.DoctorModule
{
    public class Specialty : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        private readonly List<Doctor> _doctors = [];
        public IReadOnlyCollection<Doctor> Doctors => _doctors.AsReadOnly(); 
        private Specialty() { }

        public Specialty(string name)
        {
            SetName(name);
        }
        public void UpdateInfo(string name)
        {
            SetName(name);
        }
        private void SetName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
        }
    }
}
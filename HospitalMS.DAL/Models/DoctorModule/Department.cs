namespace HospitalMS.DAL.Models.DoctorModule
{
    public class Department:BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        private readonly List<Doctor> _doctors = [];
        public IReadOnlyCollection<Doctor> Doctors => _doctors.AsReadOnly(); 
        private Department() { }
        public Department(string name)
        {
            SetName(name);
        }

        public void UpdateInfo(string name)
        {
            SetName( name);
        }
        private void SetName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
        }

    }
}

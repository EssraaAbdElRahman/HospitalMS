namespace HospitalMS.DAL.Common.Validators
{
    public static class PersonNameValidator
    {
        public static string Validate(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                throw new ArgumentException("Name is required");
            return name.Trim();
        }
    }
}

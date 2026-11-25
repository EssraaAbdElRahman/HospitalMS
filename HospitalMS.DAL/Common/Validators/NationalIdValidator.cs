
namespace HospitalMS.DAL.Common.Validators
{
    public static class NationalIdValidator
    {
        public static string Validate(string nationalId)
        {

            if (string.IsNullOrWhiteSpace(nationalId)
                || nationalId.Length != 14
                || !nationalId.All(char.IsDigit))
            {
                throw new ArgumentException("Invalid National ID");
            }

            return nationalId.Trim();
        }
    }
}

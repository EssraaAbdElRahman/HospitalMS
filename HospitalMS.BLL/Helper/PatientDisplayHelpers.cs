namespace HospitalMS.BLL.Helper
{
    public static class PatientDisplayHelpers
    {
        public static string MaskNationalId(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length < 6) return "••••••••";
            return $"{nationalId[..2]}{new string('•', nationalId.Length - 6)}{nationalId[^4..]}";
        }
    }
}

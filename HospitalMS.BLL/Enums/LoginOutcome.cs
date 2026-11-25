namespace HospitalMS.BLL.Enums
{
    public enum LoginOutcome
    {
        Succeeded,
        InvalidCredentials,
        LockedOut,
        NotActivated,
        Blocked,
        OnHold
    }
}

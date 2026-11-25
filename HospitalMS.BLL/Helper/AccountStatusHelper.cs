using HospitalMS.DAL.Enums;

namespace HospitalMS.BLL.Helpers
{
    public static class AccountStatusHelper
    {
        private static readonly Dictionary<AccountStatus, AccountStatus[]> _transitions = new()
        {
            [AccountStatus.Pending] = Array.Empty<AccountStatus>(),
            [AccountStatus.Active] = new[] { AccountStatus.OnHold, AccountStatus.Suspended },
            [AccountStatus.OnHold] = new[] { AccountStatus.Active, AccountStatus.Suspended },
            [AccountStatus.Suspended] = new[] { AccountStatus.Active, AccountStatus.OnHold }
        };

        public static IEnumerable<AccountStatus> GetAllowedTransitions(AccountStatus current)
        {
            return _transitions.TryGetValue(current, out var allowed)
                ? allowed
                : Enumerable.Empty<AccountStatus>();
        }
        public static bool CanTransitionTo(AccountStatus current, AccountStatus next)
        {
            return GetAllowedTransitions(current).Contains(next);
        }
    }
}
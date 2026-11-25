namespace HospitalMS.BLL.ModelVM.Role
{
    public class RoleDetailsViewModel
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int UsersCount { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<UserSummaryForRoleViewModel> AssignedUsers { get; set; } = new();
    }
}

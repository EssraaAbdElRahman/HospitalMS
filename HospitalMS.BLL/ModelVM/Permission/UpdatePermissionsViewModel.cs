namespace HospitalMS.BLL.ModelVM.Permission
{
    public class UpdatePermissionsViewModel
    {
        public string? RoleId { get; set; } = string.Empty;
        public List<string> Selected { get; set; } = new();

    }
}

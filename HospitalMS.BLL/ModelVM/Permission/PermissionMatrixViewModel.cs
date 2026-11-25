namespace HospitalMS.BLL.ModelVM.Permission
{
    public class PermissionMatrixViewModel
    {
        public string? RoleId { get; set; }
        public List<string> RoleNames { get; set; } = new();
        public List<ModuleGroupViewModel> Modules { get; set; } = new();
        public Dictionary<string, HashSet<string>> RolePermissions { get; set; } = new();
    }    
}

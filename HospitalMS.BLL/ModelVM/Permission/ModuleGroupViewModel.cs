namespace HospitalMS.BLL.ModelVM.Permission
{
    public class ModuleGroupViewModel
    {
        public string ModuleName { get; set; } = string.Empty;
        public List<PermissionColumnViewModel> Columns { get; set; } = new();
    }
}

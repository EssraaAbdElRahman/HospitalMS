using HospitalMS.BLL.Authorization;
using HospitalMS.BLL.ModelVM.Permission;

namespace HospitalMS.BLL.Helper
{
    public static class BuildMatrixHelper
    {
        public static List<ModuleGroupViewModel> BuildModuleMatrix()
        {

            return new()
            {
                new ModuleGroupViewModel
                {
                    ModuleName = "Patients",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Patients.View,   Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Patients.Create,    Label = "Create" },
                        new PermissionColumnViewModel { Key = Permissions.Patients.Edit,   Label = "Edit" },
                        new PermissionColumnViewModel { Key = Permissions.Patients.Delete, Label = "Delete" },
                    }
                },
                new ModuleGroupViewModel
                {
                    ModuleName = "Doctors",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Doctors.View,   Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Doctors.Create,    Label = "Create" },
                        new PermissionColumnViewModel { Key = Permissions.Doctors.Edit,   Label = "Edit" },
                        new PermissionColumnViewModel { Key = Permissions.Doctors.Delete, Label = "Delete" },
                        new PermissionColumnViewModel { Key = Permissions.Doctors.Schedule, Label = "Doctor Schedule" },
                    }
                },
                new ModuleGroupViewModel
                {
                    ModuleName = "Receptionists",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Receptionists.View,   Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Receptionists.Create,    Label = "Create" },
                        new PermissionColumnViewModel { Key = Permissions.Receptionists.Edit,   Label = "Edit" },
                        new PermissionColumnViewModel { Key = Permissions.Receptionists.Delete, Label = "Delete" },
                    }
                },
                new ModuleGroupViewModel
                {
                    ModuleName = "Appointments",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Appointments.View,   Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Appointments.Create, Label = "Create" },
                        new PermissionColumnViewModel { Key = Permissions.Appointments.Edit,   Label = "Edit" },
                        new PermissionColumnViewModel { Key = Permissions.Appointments.Delete, Label = "Delete" },
                    }
                },
                
                new ModuleGroupViewModel
                {
                    ModuleName = "Settings",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Settings.View,   Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Settings.Manage, Label = "Manage" },
                    }
                },               
                new ModuleGroupViewModel
                {
                    ModuleName = "Users",
                    Columns = new()
                    {
                        new PermissionColumnViewModel { Key = Permissions.Users.View,     Label = "View" },
                        new PermissionColumnViewModel { Key = Permissions.Users.Create,      Label = "Create" },
                        new PermissionColumnViewModel { Key = Permissions.Users.Edit,     Label = "Edit" },
                        new PermissionColumnViewModel { Key = Permissions.Users.Activate, Label = "Activate" },
                        new PermissionColumnViewModel { Key = Permissions.Users.ManageStatus,  Label = "ManageStatus" },
                    }
                },
            };
        }

    }
}

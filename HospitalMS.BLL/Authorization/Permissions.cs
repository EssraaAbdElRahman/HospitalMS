namespace HospitalMS.BLL.Authorization
{
    public static class Permissions
    {
        public static class Patients
        {
            public const string View = "Patients.View";
            public const string Create = "Patients.Create";
            public const string Edit = "Patients.Edit";
            public const string Delete = "Patients.Delete";
        }

        public static class Doctors
        {
            public const string View = "Doctors.View";
            public const string Create = "Doctors.Create";
            public const string Edit = "Doctors.Edit";
            public const string Delete = "Doctors.Delete";
            public const string Schedule = "Doctors.Schedule";
        }
        public static class Receptionists
        {
            public const string View = "Receptionists.View";
            public const string Create = "Receptionists.Create";
            public const string Edit = "Receptionists.Edit";
            public const string Delete = "Receptionists.Delete";
        }

        public static class Appointments
        {
            public const string View = "Appointments.View";
            public const string Create = "Appointments.Create";
            public const string Edit = "Appointments.Edit";
            public const string Delete = "Appointments.Delete";
        }  
    
        public static class Settings
        {
            // Departments, Specialties
            public const string View = "Settings.View";
            public const string Manage = "Settings.Manage";
        }      

        public static class Users
        {
            public const string View = "Users.View";
            public const string Create = "Users.Create";
            public const string Edit = "Users.Edit";
            public const string Activate = "Users.Activate";
            public const string ManageStatus = "Users.ManageStatus";
        }
        
        public static IEnumerable<string> GetAll()
        {
            // Patients
            yield return Patients.View;
            yield return Patients.Create;
            yield return Patients.Edit;
            yield return Patients.Delete;
            // Doctors
            yield return Doctors.View;
            yield return Doctors.Create;
            yield return Doctors.Edit;
            yield return Doctors.Delete;
            yield return Doctors.Schedule;

            // Receptionists
            yield return Receptionists.View;
            yield return Receptionists.Create;
            yield return Receptionists.Edit;
            yield return Receptionists.Delete;

            // Appointments
            yield return Appointments.View;
            yield return Appointments.Create;
            yield return Appointments.Edit;
            yield return Appointments.Delete;

            // Settings
            yield return Settings.View;
            yield return Settings.Manage;          

            // Users
            yield return Users.View;
            yield return Users.Create;
            yield return Users.Edit;
            yield return Users.Activate;
            yield return Users.ManageStatus;
          
        }
    }
}
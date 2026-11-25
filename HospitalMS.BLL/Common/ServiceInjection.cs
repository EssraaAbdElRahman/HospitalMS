using HospitalMS.BLL.Seeding;
using HospitalMS.BLL.Service.Implementation;
using Microsoft.Extensions.DependencyInjection;
namespace HospitalMS.BLL.Common
{

    public static class ServiceInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Seeders
            services.AddScoped<IIdentitySeeder, IdentitySeeder>();
            services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
            //Account manage
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            
            // Users
            services.AddScoped<IReceptionistService, ReceptionistService>();

            // Doctor Module
            services.AddScoped<IDoctorService, DoctorService>();
            services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
            services.AddScoped<ISpecialtyService, SpecialtyService>();
            services.AddScoped<IDepartmentService, DepartmentService>();

            // Patient Module
            services.AddScoped<IPatientService, PatientService>();

            // Appointment Module
            services.AddScoped<IAppointmentService, AppointmentService>();         
            
          

            return services;
        }

    }
}

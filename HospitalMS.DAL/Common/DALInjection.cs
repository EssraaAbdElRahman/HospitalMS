using HospitalMS.DAL.Repositories.Implementation;
using HospitalMS.DAL.UnitOfWorks;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalMS.DAL.Common
{
    public static class DALInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {

            // Receptionist
            services.AddScoped<IReceptionistRepository, ReceptionistRepository>();

            // Doctor Module
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
            services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            // Patient Module
            services.AddScoped<IPatientRepository, PatientRepository>();

            // Appointment Module
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            //
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}

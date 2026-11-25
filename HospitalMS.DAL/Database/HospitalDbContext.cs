using HospitalMS.DAL.Models.AppModule;
using HospitalMS.DAL.Models.AppointmentModule;
using HospitalMS.DAL.Models.PatientModule;
using HospitalMS.DAL.Models.ReceptionistModule;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace HospitalMS.DAL.Database
{
    public class HospitalDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public HospitalDbContext(DbContextOptions<HospitalDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(HospitalDbContext).Assembly);

        }

        //Users
        public DbSet<Receptionist> Receptionists { get; set; }

        // Doctor Module
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }

        // Patient Module
        public DbSet<Patient> Patients { get; set; }
     
        // Appointment Module
        public DbSet<Appointment> Appointments { get; set; }

    }
}


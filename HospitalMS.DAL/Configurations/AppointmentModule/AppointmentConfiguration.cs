using HospitalMS.DAL.Models.AppointmentModule;

namespace HospitalMS.DAL.Configurations.AppointmentModule
{
    public class AppointmentConfiguration : BaseEntityConfiguration<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);

            builder.ToTable("Appointments");
            builder.Property(x => x.AppointmentDate)
                .IsRequired();
            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.AppointmentType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.PatientId)
                   .IsRequired()
                   .HasMaxLength(maxLength: 450);

            builder.Property(x => x.DoctorId)
                .IsRequired()
                .HasMaxLength(450);     
            builder.HasOne(x => x.Patient)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
             
            builder.HasOne(x => x.Doctor)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a =>new { a.DoctorId, a.AppointmentDate });

        }
    }
}

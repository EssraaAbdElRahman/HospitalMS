using HospitalMS.DAL.Models.PatientModule;
namespace HospitalMS.DAL.Configurations.PatientModule
{
    public class PatientConfiguration : BaseEntityConfiguration<Patient>
    {
        public override void Configure(EntityTypeBuilder<Patient> builder)
        {
            base.Configure(builder);
            builder.ToTable("Patients");
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.NationalId)
                .IsRequired()
                .HasMaxLength(14);

            builder.HasIndex(x => x.NationalId)
                   .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            builder.Property(x => x.Gender)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(x => x.Address)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Country)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);
            builder.Property(x => x.UserId)
                .HasMaxLength(450);
            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<Patient>(x => x.UserId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
            builder.Navigation(x => x.Appointments)
                   .HasField("_appointment")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}

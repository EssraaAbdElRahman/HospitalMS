namespace HospitalMS.DAL.Configurations.DoctorModule
{
    public class DoctorConfiguration : BaseEntityConfiguration<Doctor>
    {
        public override void Configure(EntityTypeBuilder<Doctor> builder)
        {
            base.Configure(builder);
            builder.ToTable("Doctors");
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);
            builder.Property(x => x.Bio)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.YearsOfExperience)
                .IsRequired();

            builder.Property(x => x.NationalId)
                .IsRequired()
                .HasMaxLength(14);

            builder.HasIndex(x => x.NationalId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(x => x.HireDate)
                .IsRequired();

            builder.Property(x => x.Salary)
                .IsRequired()
                .HasPrecision(18, 2);
            builder.Property(x => x.DepartmentId)
                .IsRequired()
                .HasMaxLength(450);
            builder.Property(x => x.SpecialtyId)
                .IsRequired()
                .HasMaxLength(450);
            builder.Property(x => x.UserId)
                .HasMaxLength(450);
            builder.HasOne(x => x.Department)
                .WithMany(x => x.Doctors)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Specialty)
                .WithMany(x => x.Doctors)
                .HasForeignKey(x => x.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User)
                 .WithOne()
                 .HasForeignKey<Doctor>(x => x.UserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            builder.Navigation(x => x.Appointments)
                  .HasField("_appointment")
                  .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}

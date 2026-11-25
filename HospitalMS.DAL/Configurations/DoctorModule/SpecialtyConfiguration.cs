namespace HospitalMS.DAL.Configurations.DoctorModule
{
    public class SpecialtyConfiguration:BaseEntityConfiguration<Specialty>
    {
        public override void Configure(EntityTypeBuilder<Specialty> builder)
        {
            base.Configure(builder);
            builder.ToTable("Specialisties");
            builder.Property(x => x.Name)
                .IsRequired().HasMaxLength(100);

            builder.HasIndex(x => x.Name)
                   .IsUnique()
                   .HasFilter("[IsDeleted] = 0");
        }
    }
}

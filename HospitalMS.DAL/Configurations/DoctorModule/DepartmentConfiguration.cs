namespace HospitalMS.DAL.Configurations.DoctorModule
{
    public class DepartmentConfiguration : BaseEntityConfiguration<Department>
    {
        public override void Configure(EntityTypeBuilder<Department> builder)
        {
            base.Configure(builder);
            builder.ToTable("Departments");
            builder.Property(x => x.Name)
                  .IsRequired()
                 .HasMaxLength(100);
            builder.HasIndex(x => x.Name)
                   .IsUnique()
                   .HasFilter("[IsDeleted] = 0");
        }
    }
}

using HospitalMS.DAL.Models.AppModule;
namespace HospitalMS.DAL.Configurations.AppModule
{
    public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
     public   void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.Property(x => x.Description)
                          .HasMaxLength(250);
        }
    }
}

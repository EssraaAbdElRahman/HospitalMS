using HospitalMS.DAL.Models.ReceptionistModule;
namespace HospitalMS.DAL.Configurations.ReceptionistModule
{
    public class ReceptionistConfiguration : BaseEntityConfiguration<Receptionist>
    {
        public override void Configure(EntityTypeBuilder<Receptionist> builder)
        {
            base.Configure(builder);
            builder.ToTable("Receptionists");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

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
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.UserId)
                .HasMaxLength(450);

            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<Receptionist>(x => x.UserId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
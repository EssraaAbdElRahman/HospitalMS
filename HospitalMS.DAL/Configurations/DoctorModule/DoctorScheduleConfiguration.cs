namespace HospitalMS.DAL.Configurations.DoctorModule
{
    public class DoctorScheduleConfiguration : BaseEntityConfiguration<DoctorSchedule>
    {
        public override void Configure(EntityTypeBuilder<DoctorSchedule> builder)
        {
            base.Configure(builder);
            builder.ToTable("DoctorSchedules");
            builder.Property(x => x.Days)
              .IsRequired()
              .HasMaxLength(200);
            builder.Property(x => x.StartTime)
                   .IsRequired();

            builder.Property(x => x.EndTime)
                  .IsRequired();

            builder.HasOne(x => x.Doctor)
                  .WithMany(x => x.DoctorSchedules)
                  .HasForeignKey(x => x.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace HospitalMS.BLL.Seeding
{
    public interface IDemoDataSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}

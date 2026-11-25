namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface ISpecialtyRepository : IRepository<Specialty>
    {
        Task<(List<Specialty> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<Specialty?> GetByNameAsync(string name, CancellationToken ct);
    }
}
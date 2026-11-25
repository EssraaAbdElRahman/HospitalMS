namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<(List<Department> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<Department?> GetByNameAsync(string name, CancellationToken ct);
    }
}

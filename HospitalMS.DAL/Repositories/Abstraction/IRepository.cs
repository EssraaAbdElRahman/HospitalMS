namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<List<T>> GetAllAsync(CancellationToken ct);
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
        Task AddAsync(T entity, string createdBy, CancellationToken ct);
        Task UpdateAsync(T entity, string updatedBy, CancellationToken ct);
        Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
    }
}

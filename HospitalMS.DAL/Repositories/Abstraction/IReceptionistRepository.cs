using HospitalMS.DAL.Models.ReceptionistModule;
namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IReceptionistRepository : IRepository<Receptionist>
    {
        Task AssignUserAsync(Guid receptionistId, string userId, string updatedBy, CancellationToken ct);
        Task<(List<Receptionist> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<Receptionist?> GetByNationalIdAsync(string nationalId, CancellationToken ct);
        Task ChangeSalaryAsync(Guid receptionistId, decimal salary, string updatedBy, CancellationToken ct);

    }
}
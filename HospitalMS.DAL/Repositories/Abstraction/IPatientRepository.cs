using HospitalMS.DAL.Models.PatientModule;

namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct);
        Task<(List<Patient> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<Patient?> GetByNameAsync(string name, CancellationToken ct);
        Task<List<Patient>> SearchAsync(string? searchTerm, CancellationToken ct);
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> patientIds, CancellationToken ct);
        Task<bool> ExistsByNationalIdAsync(string nationalId, Guid? currentPatientId = null, CancellationToken ct = default);
    }
}
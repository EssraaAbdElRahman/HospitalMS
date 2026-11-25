namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<(List<Doctor> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<Doctor?> GetDetailsByIdAsync(Guid id, CancellationToken ct );
        Task ChangeSalaryAsync(Guid doctorId, decimal salary, string updatedBy, CancellationToken ct);

        Task ChangeDepartmentAsync(Guid doctorId, Guid departmentId, string updatedBy, CancellationToken ct);

        Task ChangeSpecialtyAsync(Guid doctorId, Guid specialtyId, string updatedBy, CancellationToken ct);

        Task AssignUserAsync(Guid doctorId, string userId, string updatedBy, CancellationToken ct);

        Task<Doctor?> GetByNationalIdAsync(string nationalId, CancellationToken ct);
        Task<Doctor?> GetByUserIdAsync(string userId, CancellationToken ct);

        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> doctorIds, CancellationToken ct);
    }
}

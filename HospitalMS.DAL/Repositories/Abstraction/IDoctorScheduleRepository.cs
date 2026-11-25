namespace HospitalMS.DAL.Repositories.Abstraction
{
    public interface IDoctorScheduleRepository : IRepository<DoctorSchedule>
    {
        Task<(List<DoctorSchedule> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<DoctorSchedule?> GetDetailsByIdAsync(Guid id, CancellationToken ct);
        Task<bool> HasConflictAsync(List<Days> days, Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludedId = null, CancellationToken ct = default);
        Task<List<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct);
        Task DeleteByDoctorIdAsync(Guid doctorId, string deletedBy, CancellationToken ct);
    }
}

using HospitalMS.DAL.Models.ReceptionistModule;

namespace HospitalMS.DAL.Repositories.Implementation
{
    public class ReceptionistRepository : IReceptionistRepository
    {
        private readonly HospitalDbContext _context;
        public ReceptionistRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Receptionist entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Receptionists.AddAsync(entity, ct);
        }
        public async Task AssignUserAsync(Guid receptionistId, string userId, string updatedBy, CancellationToken ct)
        {
           var receptionist = await _context.Receptionists
                           .FirstOrDefaultAsync(r => r.Id == receptionistId
                           , ct);
            if (receptionist is null)
                return;

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            receptionist.AssignUser(userId);
            receptionist.Updated(updatedBy);
        }
        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var receptionist = await _context.Receptionists
                                            .FirstOrDefaultAsync(r => r.Id == id
                                            , ct);
            if (receptionist is null)
                return;
            receptionist.Deleted(deletedBy);
        }
        public async Task<List<Receptionist>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Receptionists.AsNoTracking()
                                 .ToListAsync(ct);
        }

        public async Task<Receptionist?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Receptionists.AsNoTracking()
                                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<Receptionist?> GetByNationalIdAsync(string nationalId, CancellationToken ct)
        {
            return await _context.Receptionists.AsNoTracking()
                                .FirstOrDefaultAsync(r => r.NationalId == nationalId, ct);
        }
        public async Task<(List<Receptionist> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.Receptionists
                                            .AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.Name.Contains(search));
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                                .OrderBy(r => r.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync(ct);
            return (items, totalCount);
        }
        public async Task UpdateAsync(Receptionist entity, string updatedBy, CancellationToken ct)
        {
            var receptionist = await _context.Receptionists
                                                   .FirstOrDefaultAsync(r => r.Id == entity.Id
                                                   , ct);
            if (receptionist is null)
                return;
            receptionist.UpdateInfo(entity.Name, entity.NationalId, entity.Salary);
            receptionist.Updated(updatedBy);
        }
        public async Task ChangeSalaryAsync(Guid receptionistId, decimal salary, string updatedBy, CancellationToken ct)
        {
            var receptionist = await GetByIdWithTrackingAsync(receptionistId, ct );
            if (receptionist == null)
                throw new InvalidOperationException($"Receptionist with Id {receptionistId} was not found.");

            receptionist.ChangeSalary(salary);
            receptionist.Updated(updatedBy);
        }
        private async Task<Receptionist?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct)
        {
            return await _context.Receptionists.FirstOrDefaultAsync(r => r.Id == id, ct);
        }
    }
}
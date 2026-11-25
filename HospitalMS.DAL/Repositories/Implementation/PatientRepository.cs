using HospitalMS.DAL.Models.PatientModule;

namespace HospitalMS.DAL.Repositories.Implementation
{
    public class PatientRepository : IPatientRepository
    {
        private readonly HospitalDbContext _context;
        public PatientRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Patient entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Patients.AddAsync(entity, ct);
        }

        public async Task AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct)
        {
            var patient = await _context.Patients
                                       .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (patient is null)
                return;

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            patient.AssignUser(userId);
            patient.Updated(updatedBy);
        }
        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var patient = await _context.Patients
                                                      .FirstOrDefaultAsync(p => p.Id == id
                                                      , ct);
            if (patient is null)
                return;
            patient.Deleted(deletedBy);
        }

        public async Task<bool> ExistsByNationalIdAsync(string nationalId, Guid? currentPatientId = null, CancellationToken ct = default)
        {
            if (currentPatientId.HasValue)
            {
                return await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(p => p.NationalId == nationalId.Trim() && p.Id != currentPatientId.Value, ct);
            }
            return await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.NationalId == nationalId.Trim(), ct);
        }

        public async Task<List<Patient>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Patients.AsNoTracking()
                                            .ToListAsync(ct);
        }

        public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Patients.AsNoTracking()
                                            .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Patient?> GetByNameAsync(string name, CancellationToken ct)
        {
            return await _context.Patients
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(p => p.Name == name, ct);
        }

        public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> patientIds, CancellationToken ct)
        {
            if (!patientIds.Any())
                return new Dictionary<Guid, string>();

            return await _context.Patients
                                 .Where(p => patientIds.Contains(p.Id))
                                 .AsNoTracking()
                                 .ToDictionaryAsync(p => p.Id, p => p.Name);
        }

        public async Task<(List<Patient> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.Patients
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
        public async Task<List<Patient>> SearchAsync(string? searchTerm, CancellationToken ct)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    p.NationalId.Contains(searchTerm));
            }

            return await query.OrderBy(p => p.Name).Take(50).ToListAsync(ct);
        }
        public async Task UpdateAsync(Patient entity, string updatedBy, CancellationToken ct)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == entity.Id, ct);
            if (patient is null)
                return;

            patient.UpdateInfo(entity.Name, entity.Gender, entity.Address
                , entity.Country, entity.City, entity.Notes);
            patient.Updated(updatedBy);
        }
    }
}
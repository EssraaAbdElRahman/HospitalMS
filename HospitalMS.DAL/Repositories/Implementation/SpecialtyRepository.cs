namespace HospitalMS.DAL.Repositories.Implementation
{
    public class SpecialtyRepository : ISpecialtyRepository
    {
        private readonly HospitalDbContext _context;
        public SpecialtyRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Specialty entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Specialties.AddAsync(entity, ct);
        }
        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(s => s.Id == id, ct);
            specialty?.Deleted(deletedBy);
        }
        public async Task<List<Specialty>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Specialties.AsNoTracking().ToListAsync(ct);
        }
        public async Task<Specialty?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Specialties.AsNoTracking()
                                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public async Task<Specialty?> GetByNameAsync(string name, CancellationToken ct)
        {
            return await _context.Specialties.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == name, ct);
        }
        public async Task<(List<Specialty> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.Specialties
                                            .AsNoTracking()
                                            .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Name.Contains(search));
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                                .OrderBy(s => s.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .AsNoTracking()
                                .ToListAsync(ct);
            return (items, totalCount);
        }
        public async Task UpdateAsync(Specialty entity, string updatedBy, CancellationToken ct)
        {
            var specialty = await _context.Specialties
                                                   .FirstOrDefaultAsync(s => s.Id == entity.Id,
                                                   ct);
            if (specialty is null)
                return;
            specialty.UpdateInfo(entity.Name);
            specialty.Updated(updatedBy);
        }
    }
}
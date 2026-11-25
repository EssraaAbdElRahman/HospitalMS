namespace HospitalMS.DAL.Repositories.Implementation
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly HospitalDbContext _context;

        public DepartmentRepository(HospitalDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Department entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Departments.AddAsync(entity, ct);
        }
        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var department = await _context.Departments
                                                    .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (department is null)
                return;
            department.Deleted(deletedBy);
        }
        public async Task<List<Department>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Departments.AsNoTracking()
                                 .ToListAsync(ct);
        }
        public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Departments.AsNoTracking()
                                .FirstOrDefaultAsync(d => d.Id == id, ct);
        }
        public async Task<Department?> GetByNameAsync(string name, CancellationToken ct)
        {
            return await _context.Departments.AsNoTracking()
                            .FirstOrDefaultAsync(d => d.Name == name, ct);
        }
        public async Task<(List<Department> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.Departments
                                            .AsNoTracking()
                                            .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.Name.Contains(search));
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                                .OrderBy(d => d.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .AsNoTracking()
                                .ToListAsync(ct);
            return (items, totalCount);
        }
        public async Task UpdateAsync(Department entity, string updatedBy, CancellationToken ct)
        {
            var department = await _context.Departments
                                                   .FirstOrDefaultAsync(d => d.Id == entity.Id,ct);
            if (department is null)
                return;
            department.UpdateInfo(entity.Name);
            entity.Updated(updatedBy);
        }
    }
}
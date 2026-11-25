
namespace HospitalMS.DAL.Repositories.Implementation
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly HospitalDbContext _context;
        public DoctorRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Doctor entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.Doctors.AddAsync(entity, ct);
        }

        public async Task AssignUserAsync(Guid doctorId, string userId, string updatedBy, CancellationToken ct)
        {
            var doctor = await _context.Doctors
                                     .FirstOrDefaultAsync(d => d.Id == doctorId
                                     , ct);
            if (doctor is null)
                return;

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            doctor.AssignUser(userId);
            doctor.Updated(updatedBy);
        }

        public async Task ChangeDepartmentAsync(Guid doctorId, Guid departmentId, string updatedBy, CancellationToken ct)
        {
            var doctor = await GetByIdWithTrackingAsync(doctorId, ct);
            if (doctor == null)
                throw new InvalidOperationException($"Doctor with Id {doctorId} was not found.");

            doctor.ChangeDepartment(departmentId);
            doctor.Updated(updatedBy);
        }

        public async Task ChangeSalaryAsync(Guid doctorId, decimal salary, string updatedBy, CancellationToken ct)
        {
            var doctor = await GetByIdWithTrackingAsync(doctorId, ct);
            if (doctor == null)
                throw new InvalidOperationException($"Doctor with Id {doctorId} was not found.");

            doctor.ChangeSalary(salary);
            doctor.Updated(updatedBy);
        }

        public async Task ChangeSpecialtyAsync(Guid doctorId, Guid specialtyId, string updatedBy, CancellationToken ct)
        {
            var doctor = await GetByIdWithTrackingAsync(doctorId, ct);
            if (doctor == null)
                throw new InvalidOperationException($"Doctor with Id {doctorId} was not found.");

            doctor.ChangeSpecialty(specialtyId);
            doctor.Updated(updatedBy);
        }
        private async Task<Doctor?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct)
        {
            return await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct);
        }
        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var doctor = await _context.Doctors
                                                .FirstOrDefaultAsync(d => d.Id == id
                                                , ct);
            if (doctor is null)
                return;
            doctor.Deleted(deletedBy);
        }

        public async Task<List<Doctor>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Doctors.AsNoTracking().ToListAsync(ct);
        }
        public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .Include(d => d.DoctorSchedules)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
        }
        public async Task<Doctor?> GetByNationalIdAsync(string nationalId, CancellationToken ct)
        {
            return await _context.Doctors.AsNoTracking().Include(d => d.Department).Include(d => d.Specialty)
                                         .FirstOrDefaultAsync(d => d.NationalId == nationalId, ct);
        }
        public async Task<Doctor?> GetByUserIdAsync(string userId, CancellationToken ct)
        {
            return await _context.Doctors.AsNoTracking()
                                         .FirstOrDefaultAsync(d => d.UserId == userId, ct);
        }
        public async Task<Doctor?> GetDetailsByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .Include(d => d.Specialty)
                .Include(d => d.DoctorSchedules)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
        }
        public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> doctorIds, CancellationToken ct)
        {
            if (!doctorIds.Any())
                return new Dictionary<Guid, string>();

            return await _context.Doctors
                                 .Where(p => doctorIds.Contains(p.Id))
                                 .AsNoTracking()
                                 .ToDictionaryAsync(p => p.Id, p => p.Name);

        }
        public async Task<(List<Doctor> Items, int TotalCount)> GetPagedAsync(
     string? search,
     int page,
     int pageSize,
     CancellationToken ct)
        {
            var query = _context.Doctors
                .AsNoTracking()
                .Include(d => d.Specialty)
                .Include(d => d.Department)
                .Where(d => !d.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.Name.Contains(search) ||
                    d.NationalId.Contains(search));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
        public async Task UpdateAsync(Doctor entity, string updatedBy, CancellationToken ct)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == entity.Id, ct);
            if (doctor is null)
                return;

            doctor.UpdateInfo(entity.Name, entity.NationalId, entity.Bio
                , entity.SpecialtyId, entity.YearsOfExperience
                , entity.DepartmentId,
                entity.HireDate, entity.Salary);
            doctor.Updated(updatedBy);
        }
    }
}
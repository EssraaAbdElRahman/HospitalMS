namespace HospitalMS.DAL.Repositories.Implementation
{
    public class DoctorScheduleRepository : IDoctorScheduleRepository
    {
        private readonly HospitalDbContext _context;
        public DoctorScheduleRepository(HospitalDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(DoctorSchedule entity, string createdBy, CancellationToken ct)
        {
            entity.Created(createdBy);
            await _context.DoctorSchedules.AddAsync(entity, ct);
        }

        public async Task DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var schedule = await _context.DoctorSchedules.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (schedule is null)
                return;

            schedule.Deleted(deletedBy);
        }

        public async Task DeleteByDoctorIdAsync(Guid doctorId, string deletedBy, CancellationToken ct)
        {
            var schedules = await _context.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync(ct);
            foreach (var schedule in schedules)
            {
                schedule.Deleted(deletedBy);
            }
        }
        public async Task<List<DoctorSchedule>> GetAllAsync(CancellationToken ct)
        {
            return await _context.DoctorSchedules
                .AsNoTracking()
                .Include(s => s.Doctor)
                .ToListAsync(ct);
        }
        public async Task<List<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct)
        {
            return await _context.DoctorSchedules.AsNoTracking().Where(s => s.DoctorId == doctorId).ToListAsync(ct);
        }

        public async Task<DoctorSchedule?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.DoctorSchedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public async Task<DoctorSchedule?> GetDetailsByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.DoctorSchedules
                .AsNoTracking()
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public async Task<(List<DoctorSchedule> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.DoctorSchedules.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.DoctorId.ToString().Contains(search));
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                                .Include(schedule => schedule.Doctor)
                                .OrderBy(r => r.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync(ct);
            return (items, totalCount);
        }
        public async Task<bool> HasConflictAsync(List<Days> days, Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludedId = null, CancellationToken ct = default)
        {
            var newStart = startTime.TimeOfDay;
            var newEnd = endTime.TimeOfDay;
            var schedules = await _context.DoctorSchedules
                                                                  .AsNoTracking()
                                                                  .Where(x => x.DoctorId == doctorId && !x.IsDeleted
                                                                  && (excludedId == null || x.Id != excludedId))
                                                                  .ToListAsync(ct);

            return schedules.Any(x => x.Days
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => Enum.Parse<Days>(d.Trim()))
                    .Any(existingDay => days.Contains(existingDay))
                && newStart < x.EndTime.TimeOfDay
                && newEnd > x.StartTime.TimeOfDay);
        }

        public async Task UpdateAsync(DoctorSchedule entity, string updatedBy, CancellationToken ct)
        {
            var schedule = await GetByIdAsync(entity.Id,ct);
            if (schedule is null)
                return;
            schedule.UpdateInfo(schedule.Days, schedule.DoctorId, schedule.StartTime, schedule.EndTime);
            schedule.Updated(updatedBy);
        }
    }
}
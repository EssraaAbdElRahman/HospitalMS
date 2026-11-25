using HospitalMS.DAL.Repositories.Implementation;
using Microsoft.EntityFrameworkCore.Storage;

namespace HospitalMS.DAL.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HospitalDbContext _db;
        private IDbContextTransaction? _transaction;
        private IAppointmentRepository? _appointments;
        private IDepartmentRepository? _departments;
        private IDoctorRepository? _doctors;
        private IDoctorScheduleRepository? _doctorSchedules;
        private IPatientRepository? _patients;
        private IReceptionistRepository? _receptionists;
        private ISpecialtyRepository? _specialties;
        public UnitOfWork(HospitalDbContext db)
        {
            _db = db;
        }
        public IAppointmentRepository Appointments => _appointments ??= new AppointmentRepository(_db);
        public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(_db);
        public IDoctorRepository Doctors => _doctors ??= new DoctorRepository(_db);
        public IDoctorScheduleRepository DoctorSchedules => _doctorSchedules ??= new DoctorScheduleRepository(_db);
        public IPatientRepository Patients => _patients ??= new PatientRepository(_db);
        public IReceptionistRepository Receptionists => _receptionists ??= new ReceptionistRepository(_db);
        public ISpecialtyRepository Specialties => _specialties ??= new SpecialtyRepository(_db);
        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }
            _transaction = await _db.Database.BeginTransactionAsync(ct);
        }
        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.CommitAsync(ct);
            }
            catch
            {
                await _transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.RollbackAsync(ct);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        public async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return await _db.SaveChangesAsync(ct);
        }
        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
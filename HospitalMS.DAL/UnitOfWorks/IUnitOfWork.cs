namespace HospitalMS.DAL.UnitOfWorks
{
    public interface IUnitOfWork : IDisposable
    {

        IAppointmentRepository Appointments { get; }
        IDepartmentRepository Departments { get; }
        IDoctorRepository Doctors { get; }
        IDoctorScheduleRepository DoctorSchedules { get; }
        IPatientRepository Patients { get; }
        IReceptionistRepository Receptionists { get; }
        ISpecialtyRepository Specialties { get; }
        Task<int> SaveChangesAsync(CancellationToken ct);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}

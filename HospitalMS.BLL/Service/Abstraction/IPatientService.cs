using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Patient;
using HospitalMS.DAL.Models.PatientModule;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IPatientService
    {
        Task<List<PatientListItemViewModel>> GetAllAsync( CancellationToken ct);
        Task<UpdatePatientViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<PatientDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> patientIds, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewPatientViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdatePatientViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
        Task<ServiceResult> AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct);
        Task<PagedResult<PatientListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<List<Patient>> SearchAsync(string? searchTerm, CancellationToken ct);


    }
}

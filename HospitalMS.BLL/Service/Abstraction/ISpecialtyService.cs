using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Specialization;
using HospitalMS.BLL.ModelVM.Specialty;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface ISpecialtyService
    {
        Task<List<SpecialtyListItemViewModel>> GetAllAsync( CancellationToken ct);
        Task<PagedResult<SpecialtyListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<UpdateSpecialtyViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<SpecializationDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewSpecialtyViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateSpecialtyViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
    }
}

using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Receptionist;
using HospitalMS.DAL.Models.ReceptionistModule;

namespace HospitalMS.BLL.Service.Abstraction
{
    public interface IReceptionistService
    {
        Task<List<ReceptionistListItemViewModel>> GetAllAsync(CancellationToken ct);
        Task<List<ReceptionistUnAssignedUserViewModel>> GetAllUnassignedAsync(CancellationToken ct);

        Task<PagedResult<ReceptionistListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
        Task<UpdateReceptionistViewModel?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<ReceptionistDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<ServiceResult<Guid>> CreateAsync(NewReceptionistViewModel vm, string createdBy, CancellationToken ct);
        Task<ServiceResult> UpdateAsync(UpdateReceptionistViewModel vm, string updatedBy, CancellationToken ct);
        Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct);
        Task<ServiceResult> AssignUserAsync(Guid id, string userId, string updatedBy, CancellationToken ct);
        Task<Receptionist?> GetByNationalIdAsync(string nationalId, CancellationToken ct);
    }
}

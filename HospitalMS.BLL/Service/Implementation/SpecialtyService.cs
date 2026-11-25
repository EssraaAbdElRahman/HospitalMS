using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.BLL.ModelVM.Specialization;
using HospitalMS.BLL.ModelVM.Specialty;
using HospitalMS.DAL.Models.DoctorModule;
using Microsoft.EntityFrameworkCore;

namespace HospitalMS.BLL.Service.Implementation
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SpecialtyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<SpecialtyListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 5;

            var result = await _unitOfWork.Specialties.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<SpecialtyListItemViewModel>
            {
                Items = result.Items.Select(specialty => new SpecialtyListItemViewModel
                {
                    Id = specialty.Id,
                    Name = specialty.Name,
                    CreatedAt = specialty.CreatedAt,
                    IsDeleted = specialty.IsDeleted
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<SpecializationDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var specialty = await _unitOfWork.Specialties.GetByIdAsync(id, ct);

            if (specialty is null || specialty.IsDeleted)
                return null;

            return new SpecializationDetailsViewModel
            {
                Id = specialty.Id,
                Name = specialty.Name,
                CreatedAt = specialty.CreatedAt
            };
        }

        public async Task<List<SpecialtyListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Specialties.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(x => new SpecialtyListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt,
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public async Task<UpdateSpecialtyViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var specialty = await _unitOfWork.Specialties.GetByIdAsync(id, ct);

            if (specialty is null || specialty.IsDeleted)
                return null;

            return new UpdateSpecialtyViewModel
            {
                Id = specialty.Id,
                Name = specialty.Name
            };
        }

        public async Task<ServiceResult<Guid>> CreateAsync(NewSpecialtyViewModel vm, string createdBy, CancellationToken ct)
        {
            var existing = await _unitOfWork.Specialties.GetByNameAsync(vm.Name, ct);

            if (existing != null)
                return ServiceResult<Guid>.Failure("Specialty name already exists.");

            try
            {
                var specialty = new Specialty(vm.Name);

                await _unitOfWork.Specialties.AddAsync(specialty, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(specialty.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<Guid>.Failure("Specialty name already exists.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(UpdateSpecialtyViewModel vm, string updatedBy, CancellationToken ct)
        {
            var specialty = await _unitOfWork.Specialties.GetByIdAsync(vm.Id, ct);

            if (specialty is null || specialty.IsDeleted)
                return ServiceResult.Failure("Specialty not found.");

            var existing = await _unitOfWork.Specialties.GetByNameAsync(vm.Name, ct);

            if (existing != null && existing.Id != vm.Id)
                return ServiceResult.Failure("Specialty name already exists.");

            try
            {
                specialty.UpdateInfo(vm.Name);

                await _unitOfWork.Specialties.UpdateAsync(specialty, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Failure("Specialty name already exists.");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var specialty = await _unitOfWork.Specialties.GetByIdAsync(id, ct);

            if (specialty is null || specialty.IsDeleted)
                return ServiceResult.Failure("Specialty not found.");

            try
            {
                await _unitOfWork.Specialties.DeleteAsync(id, deletedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }
    }
}
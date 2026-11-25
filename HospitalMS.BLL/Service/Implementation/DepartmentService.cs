using HospitalMS.BLL.ModelVM.Department;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.DAL.Models.DoctorModule;
using Microsoft.EntityFrameworkCore;

namespace HospitalMS.BLL.Service.Implementation
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepartmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<DepartmentListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 7;

            var result = await _unitOfWork.Departments.GetPagedAsync(search, page, pageSize, ct);

            return new PagedResult<DepartmentListItemViewModel>
            {
                Items = result.Items.Select(department => new DepartmentListItemViewModel
                {
                    Id = department.Id,
                    Name = department.Name
                }),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                SearchTerm = search
            };
        }

        public async Task<List<DepartmentListItemViewModel>> GetAllAsync(CancellationToken ct)
        {
            var items = await _unitOfWork.Departments.GetAllAsync(ct);

            return items
                .Where(x => !x.IsDeleted)
                .Select(x => new DepartmentListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt,
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public async Task<UpdateDepartmentViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id, ct);

            if (department is null || department.IsDeleted)
                return null;

            return new UpdateDepartmentViewModel
            {
                Id = department.Id,
                Name = department.Name
            };
        }

        public async Task<DepartmentDetailsViewModel?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id, ct);

            if (department is null || department.IsDeleted)
                return null;

            return new DepartmentDetailsViewModel
            {
                Id = department.Id,
                Name = department.Name,
                CreatedAt = department.CreatedAt
            };
        }

        public async Task<ServiceResult<Guid>> CreateAsync(NewDepartmentViewModel vm, string createdBy, CancellationToken ct)
        {
            var existing = await _unitOfWork.Departments.GetByNameAsync(vm.Name, ct);

            if (existing != null)
                return ServiceResult<Guid>.Failure("Department name already exists.");

            try
            {
                var department = new Department(vm.Name);

                await _unitOfWork.Departments.AddAsync(department, createdBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<Guid>.Success(department.Id);
            }
            catch (ArgumentException ex)
            {
                return ServiceResult<Guid>.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<Guid>.Failure("Department name already exists.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(UpdateDepartmentViewModel vm, string updatedBy, CancellationToken ct)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(vm.Id, ct);

            if (department is null || department.IsDeleted)
                return ServiceResult.Failure("Department not found.");

            var existing = await _unitOfWork.Departments.GetByNameAsync(vm.Name, ct);

            if (existing != null && existing.Id != vm.Id)
                return ServiceResult.Failure("Department name already exists.");

            try
            {
                department.UpdateInfo(vm.Name);

                await _unitOfWork.Departments.UpdateAsync(department, updatedBy, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Failure("Department name already exists.");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id, ct);

            if (department is null || department.IsDeleted)
                return ServiceResult.Failure("Department not found.");

            try
            {
                await _unitOfWork.Departments.DeleteAsync(id, deletedBy, ct);
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
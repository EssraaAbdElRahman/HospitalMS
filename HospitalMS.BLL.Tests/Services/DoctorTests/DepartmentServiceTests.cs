using System.Reflection;
using HospitalMS.BLL.ModelVM.Department;
using HospitalMS.BLL.ModelVM.Pagination;
using HospitalMS.DAL.Models.DoctorModule;
namespace HospitalMS.BLL.Tests.Services.DoctorTests
{
    public class DepartmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IDepartmentRepository> _repo = new();
        private readonly DepartmentService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;
        private const string NotFoundMsg = "Department not found.";
        private const string DuplicateMsg = "Department name already exists.";

        public DepartmentServiceTests()
        {
            _uowMock.Setup(u => u.Departments).Returns(_repo.Object);
            _service = new DepartmentService(_uowMock.Object);
        }

        // ---------------- Helpers ----------------

        private static void SetProp(object obj, string name, object? value)
        {
            var prop = obj.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            prop = prop.DeclaringType!.GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            prop.SetValue(obj, value);
        }

        private static Department CreateDepartment(
            string name = "Cardiology",
            Guid? id = null,
            bool isDeleted = false)
        {
            var d = new Department(name);
            SetProp(d, "Id", id ?? Guid.NewGuid());
            SetProp(d, "IsDeleted", isDeleted);
            return d;
        }

        private void SetupGetById(Guid id, Department? department) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(department);

        private void SetupGetByName(string name, Department? department) =>
            _repo.Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(department);

        private void VerifyNotSaved() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        private void SetupPaged(string? search, int page, int pageSize, int total, params Department[] items) =>
                   _repo.Setup(r => r.GetPagedAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((items.ToList(), total));

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var d1 = CreateDepartment("Cardiology");
            var d2 = CreateDepartment("Neurology");
            SetupPaged("card", 2, 5, 12, d1, d2);

            var result = await _service.GetPagedAsync("card", 2, 5, Ct);

            Assert.Equal(new[] { "Cardiology", "Neurology" }, result.Items.Select(i => i.Name));
            Assert.Equal(new[] { d1.Id, d2.Id }, result.Items.Select(i => i.Id));
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(12, result.TotalCount);
            Assert.Equal("card", result.SearchTerm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task GetPagedAsync_PageLessThanOne_DefaultsToOne(int page)
        {
            SetupPaged(null, 1, 5, 0);

            var result = await _service.GetPagedAsync(null, page, 5, Ct);

            Assert.Equal(1, result.CurrentPage);
            _repo.Verify(r => r.GetPagedAsync(null, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPagedAsync_InvalidPageSize_DefaultsToSeven(int pageSize)
        {
            SetupPaged(null, 1, 7, 0);

            var result = await _service.GetPagedAsync(null, 1, pageSize, Ct);

            Assert.Equal(7, result.PageSize);
            _repo.Verify(r => r.GetPagedAsync(null, 1, 7, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_NoItems_ReturnsEmpty()
        {
            SetupPaged(null, 1, 7, 0);

            var result = await _service.GetPagedAsync(null, 1, 7, Ct);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoDepartments_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department>());

            var result = await _service.GetAllAsync(Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeleted()
        {
            var active = CreateDepartment("Cardiology");
            var deleted = CreateDepartment("Old", isDeleted: true);
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(active.Id, item.Id);
            Assert.Equal("Cardiology", item.Name);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department> { CreateDepartment(isDeleted: true) });

            var result = await _service.GetAllAsync(Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_MapsCreatedAt()
        {
            var d = CreateDepartment();
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department> { d });

            var result = await _service.GetAllAsync(Ct);

            Assert.Equal(d.CreatedAt, result[0].CreatedAt);
        }

        // =====================================================
        //                      GetByIdAsync
        // =====================================================

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.GetByIdAsync(id, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_Deleted_ReturnsNull()
        {
            var d = CreateDepartment(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.GetByIdAsync(d.Id, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModel()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);

            var result = await _service.GetByIdAsync(d.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(d.Id, result!.Id);
            Assert.Equal("Cardiology", result.Name);
        }

        // =====================================================
        //                     GetDetailsAsync
        // =====================================================

        [Fact]
        public async Task GetDetailsAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.GetDetailsAsync(id, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDetailsAsync_Deleted_ReturnsNull()
        {
            var d = CreateDepartment(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.GetDetailsAsync(d.Id, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModel()
        {
            var d = CreateDepartment("Neurology");
            SetupGetById(d.Id, d);

            var result = await _service.GetDetailsAsync(d.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(d.Id, result!.Id);
            Assert.Equal("Neurology", result.Name);
            Assert.Equal(d.CreatedAt, result.CreatedAt);
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Fact]
        public async Task CreateAsync_NameAlreadyExists_ReturnsFailureWithoutAdding()
        {
            SetupGetByName("Cardiology", CreateDepartment("Cardiology"));
            var vm = new NewDepartmentViewModel { Name = "Cardiology" };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
            _repo.Verify(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupGetByName("Cardiology", null);
            Department? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Department>(), "admin", It.IsAny<CancellationToken>()))
                .Callback<Department, string, CancellationToken>((d, _, _) => added = d);
            var vm = new NewDepartmentViewModel { Name = "Cardiology" };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(added);
            Assert.Equal("Cardiology", added!.Name);
            Assert.Equal(added.Id, result.Data);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_InvalidName_ReturnsFailureFromEntityValidation(string? name)
        {
            // الـ constructor بتاع Department غالبًا بيرمي ArgumentException للاسم الفاضي
            _repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Department?)null);
            var vm = new NewDepartmentViewModel { Name = name! };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupGetByName("Cardiology", null);
            _repo.Setup(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid name"));
            var vm = new NewDepartmentViewModel { Name = "Cardiology" };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid name", result.Errors);
        }

        [Fact]
        public async Task CreateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            SetupGetByName("Cardiology", null);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));
            var vm = new NewDepartmentViewModel { Name = "Cardiology" };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
        }

        // =====================================================
        //                      UpdateAsync
        // =====================================================

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            var vm = new UpdateDepartmentViewModel { Id = Guid.NewGuid(), Name = "X" };
            SetupGetById(vm.Id, null);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _repo.Verify(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_Deleted_ReturnsFailure()
        {
            var d = CreateDepartment(isDeleted: true);
            SetupGetById(d.Id, d);
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "X" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_NameUsedByAnotherDepartment_ReturnsDuplicate()
        {
            var d = CreateDepartment("Cardiology");
            var other = CreateDepartment("Neurology");
            SetupGetById(d.Id, d);
            SetupGetByName("Neurology", other);
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "Neurology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NameBelongsToSameDepartment_Succeeds()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);
            SetupGetByName("Cardiology", d); // نفس الـ Id
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "Cardiology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesNameAndSaves()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);
            SetupGetByName("Oncology", null);
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "Oncology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal("Oncology", d.Name);
            _repo.Verify(r => r.UpdateAsync(d, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EmptyName_ReturnsFailureFromEntityValidation()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);
            SetupGetByName("", null);
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);
            SetupGetByName("Oncology", null);
            _repo.Setup(r => r.UpdateAsync(d, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update"));
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "Oncology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update", result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            var d = CreateDepartment("Cardiology");
            SetupGetById(d.Id, d);
            SetupGetByName("Oncology", null);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));
            var vm = new UpdateDepartmentViewModel { Id = d.Id, Name = "Oncology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
        }

        // =====================================================
        //                      DeleteAsync
        // =====================================================

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.DeleteAsync(id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeleted_ReturnsFailure()
        {
            var d = CreateDepartment(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesAndSaves()
        {
            var d = CreateDepartment();
            SetupGetById(d.Id, d);

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.DeleteAsync(d.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var d = CreateDepartment();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.DeleteAsync(d.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Department has doctors"));

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Department has doctors", result.Errors);
            VerifyNotSaved();
        }
    }
}
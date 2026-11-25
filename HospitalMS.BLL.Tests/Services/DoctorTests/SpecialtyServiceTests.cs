using HospitalMS.BLL.ModelVM.Specialty;
using HospitalMS.DAL.Models.DoctorModule;
using System.Reflection;

namespace HospitalMS.BLL.Tests.Services.DoctorTests
{
    public class SpecialtyServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ISpecialtyRepository> _repo = new();
        private readonly SpecialtyService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;
        private const string NotFoundMsg = "Specialty not found.";
        private const string DuplicateMsg = "Specialty name already exists.";

        public SpecialtyServiceTests()
        {
            _uowMock.Setup(u => u.Specialties).Returns(_repo.Object);
            _service = new SpecialtyService(_uowMock.Object);
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

        private static Specialty CreateSpecialty(
            string name = "Cardiology",
            Guid? id = null,
            bool isDeleted = false)
        {
            var s = new Specialty(name);
            SetProp(s, "Id", id ?? Guid.NewGuid());
            SetProp(s, "IsDeleted", isDeleted);
            return s;
        }

        private void SetupGetById(Guid id, Specialty? specialty) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(specialty);

        private void SetupGetByName(string name, Specialty? specialty) =>
            _repo.Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(specialty);

        private void VerifyNotSaved() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        private void SetupPaged(string? search, int page, int pageSize, int total, params Specialty[] items) =>
            _repo.Setup(r => r.GetPagedAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items.ToList(), total));

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var s1 = CreateSpecialty("Cardiology");
            var s2 = CreateSpecialty("Neurology");
            SetupPaged("card", 2, 10, 25, s1, s2);

            var result = await _service.GetPagedAsync("card", 2, 10, Ct);

            Assert.Equal(new[] { "Cardiology", "Neurology" }, result.Items.Select(i => i.Name));
            Assert.Equal(new[] { s1.Id, s2.Id }, result.Items.Select(i => i.Id));
            Assert.Equal(s1.CreatedAt, result.Items.First().CreatedAt);
            Assert.All(result.Items, i => Assert.False(i.IsDeleted));
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal("card", result.SearchTerm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task GetPagedAsync_PageLessThanOne_DefaultsToOne(int page)
        {
            SetupPaged(null, 1, 10, 0);

            var result = await _service.GetPagedAsync(null, page, 10, Ct);

            Assert.Equal(1, result.CurrentPage);
            _repo.Verify(r => r.GetPagedAsync(null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPagedAsync_InvalidPageSize_DefaultsToFive(int pageSize)
        {
            SetupPaged(null, 1, 5, 0);

            var result = await _service.GetPagedAsync(null, 1, pageSize, Ct);

            Assert.Equal(5, result.PageSize);
            _repo.Verify(r => r.GetPagedAsync(null, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_NoItems_ReturnsEmpty()
        {
            SetupPaged(null, 1, 5, 0);

            var result = await _service.GetPagedAsync(null, 1, 5, Ct);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        // =====================================================
        //                    GetDetailsAsync
        // =====================================================

        [Fact]
        public async Task GetDetailsAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            Assert.Null(await _service.GetDetailsAsync(id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Deleted_ReturnsNull()
        {
            var s = CreateSpecialty(isDeleted: true);
            SetupGetById(s.Id, s);

            Assert.Null(await _service.GetDetailsAsync(s.Id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModel()
        {
            var s = CreateSpecialty("Neurology");
            SetupGetById(s.Id, s);

            var result = await _service.GetDetailsAsync(s.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(s.Id, result!.Id);
            Assert.Equal("Neurology", result.Name);
            Assert.Equal(s.CreatedAt, result.CreatedAt);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoSpecialties_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Specialty>());

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeleted()
        {
            var active = CreateSpecialty("Cardiology");
            var deleted = CreateSpecialty("Old", isDeleted: true);
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Specialty> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(active.Id, item.Id);
            Assert.Equal("Cardiology", item.Name);
            Assert.Equal(active.CreatedAt, item.CreatedAt);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Specialty> { CreateSpecialty(isDeleted: true) });

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        // =====================================================
        //                      GetByIdAsync
        // =====================================================

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            Assert.Null(await _service.GetByIdAsync(id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Deleted_ReturnsNull()
        {
            var s = CreateSpecialty(isDeleted: true);
            SetupGetById(s.Id, s);

            Assert.Null(await _service.GetByIdAsync(s.Id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModel()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);

            var result = await _service.GetByIdAsync(s.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(s.Id, result!.Id);
            Assert.Equal("Cardiology", result.Name);
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Fact]
        public async Task CreateAsync_NameAlreadyExists_ReturnsFailureWithoutAdding()
        {
            SetupGetByName("Cardiology", CreateSpecialty("Cardiology"));
            var vm = new NewSpecialtyViewModel { Name = "Cardiology" };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
            _repo.Verify(r => r.AddAsync(It.IsAny<Specialty>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupGetByName("Cardiology", null);
            Specialty? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Specialty>(), "admin", It.IsAny<CancellationToken>()))
                .Callback<Specialty, string, CancellationToken>((s, _, _) => added = s);
            var vm = new NewSpecialtyViewModel { Name = "Cardiology" };

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
            _repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Specialty?)null);
            var vm = new NewSpecialtyViewModel { Name = name! };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupGetByName("Cardiology", null);
            _repo.Setup(r => r.AddAsync(It.IsAny<Specialty>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid name"));
            var vm = new NewSpecialtyViewModel { Name = "Cardiology" };

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
            var vm = new NewSpecialtyViewModel { Name = "Cardiology" };

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
            var vm = new UpdateSpecialtyViewModel { Id = Guid.NewGuid(), Name = "X" };
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
            var s = CreateSpecialty(isDeleted: true);
            SetupGetById(s.Id, s);
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "X" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_NameUsedByAnotherSpecialty_ReturnsDuplicate()
        {
            var s = CreateSpecialty("Cardiology");
            var other = CreateSpecialty("Neurology");
            SetupGetById(s.Id, s);
            SetupGetByName("Neurology", other);
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "Neurology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(DuplicateMsg, result.Errors);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Specialty>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NameBelongsToSameSpecialty_Succeeds()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);
            SetupGetByName("Cardiology", s);
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "Cardiology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesNameAndSaves()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);
            SetupGetByName("Oncology", null);
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "Oncology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal("Oncology", s.Name);
            _repo.Verify(r => r.UpdateAsync(s, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EmptyName_ReturnsFailureFromEntityValidation()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);
            SetupGetByName("", null);
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);
            SetupGetByName("Oncology", null);
            _repo.Setup(r => r.UpdateAsync(s, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update"));
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "Oncology" };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update", result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            var s = CreateSpecialty("Cardiology");
            SetupGetById(s.Id, s);
            SetupGetByName("Oncology", null);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));
            var vm = new UpdateSpecialtyViewModel { Id = s.Id, Name = "Oncology" };

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
            var s = CreateSpecialty(isDeleted: true);
            SetupGetById(s.Id, s);

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesAndSaves()
        {
            var s = CreateSpecialty();
            SetupGetById(s.Id, s);

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.DeleteAsync(s.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var s = CreateSpecialty();
            SetupGetById(s.Id, s);
            _repo.Setup(r => r.DeleteAsync(s.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Specialty has doctors"));

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Specialty has doctors", result.Errors);
            VerifyNotSaved();
        }
    }
}

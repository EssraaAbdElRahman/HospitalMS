using HospitalMS.BLL.ModelVM.Receptionist;
using HospitalMS.BLL.Service.Implementation;
using HospitalMS.DAL.Models.ReceptionistModule;
using Moq;
using System.Reflection;

namespace HospitalMS.BLL.Tests.Services.ReceptionistTests
{
    public class ReceptionistServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IReceptionistRepository> _repo = new();
        private readonly ReceptionistService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;

        private const string NotFoundMsg = "Receptionist not found.";
        private const string NationalIdLengthMsg = "National ID must be exactly 14 digits.";
        private const string NationalIdDuplicateMsg = "National ID already exists and must be unique.";
        private const string ValidNationalId = "12345678901234";

        public ReceptionistServiceTests()
        {
            _uowMock.Setup(u => u.Receptionists).Returns(_repo.Object);
            _service = new ReceptionistService(_uowMock.Object);
        }

        // ---------------- Helpers ----------------

        private static void SetProp(object obj, string name, object? value)
        {
            var prop = obj.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            prop = prop.DeclaringType!.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            prop.SetValue(obj, value);
        }

        private static Receptionist CreateReceptionist(
            string name = "Sara Mohamed",
            string nationalId = ValidNationalId,
            DateTime? hireDate = null,
            decimal salary = 10000m,
            Guid? id = null,
            bool isDeleted = false,
            string? userId = null)
        {
            var receptionist = new Receptionist(
                name,
                nationalId,
                hireDate ?? DateTime.Today.AddYears(-1),
                salary);

            SetProp(receptionist, "Id", id ?? Guid.NewGuid());
            SetProp(receptionist, "IsDeleted", isDeleted);

            if (userId is not null)
                SetProp(receptionist, "UserId", userId);

            return receptionist;
        }

        private void SetupGetById(Guid id, Receptionist? receptionist) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(receptionist);

        private void SetupGetByNationalId(string nationalId, Receptionist? receptionist) =>
            _repo.Setup(r => r.GetByNationalIdAsync(
                    nationalId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(receptionist);

        private void SetupPaged(
            string? search,
            int page,
            int pageSize,
            int total,
            params Receptionist[] items) =>
            _repo.Setup(r => r.GetPagedAsync(
                    search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items.ToList(), total));

        private void VerifyNotSaved() =>
            _uowMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);

        private void VerifySavedOnce() =>
            _uowMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

        private static NewReceptionistViewModel ValidNewVm() => new()
        {
            Name = "Sara Mohamed",
            NationalId = ValidNationalId,
            HireDate = DateTime.Today.AddYears(-1),
            Salary = 12000m
        };

        private static UpdateReceptionistViewModel ValidUpdateVm(Guid id) => new()
        {
            Id = id,
            Name = "Sara Updated",
            NationalId = ValidNationalId,
            HireDate = DateTime.Today.AddYears(-2),
            Salary = 15000m
        };

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var receptionist = CreateReceptionist(
                name: "Sara Ali",
                hireDate: new DateTime(2022, 5, 10),
                salary: 15000m,
                userId: "user-1");

            SetupPaged("Sara", 2, 10, 15, receptionist);

            var result = await _service.GetPagedAsync("Sara", 2, 10, Ct);

            var item = Assert.Single(result.Items);

            Assert.Equal(receptionist.Id, item.Id);
            Assert.Equal(receptionist.UserId, item.UserId);
            Assert.Equal(receptionist.Name, item.Name);
            Assert.Equal(receptionist.NationalId, item.NationalId);
            Assert.Equal(receptionist.HireDate, item.HireDate);
            Assert.Equal(receptionist.Salary, item.Salary);
            Assert.Equal(receptionist.CreatedAt, item.CreatedAt);
            Assert.Equal(receptionist.IsDeleted, item.IsDeleted);

            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(15, result.TotalCount);
            Assert.Equal("Sara", result.SearchTerm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task GetPagedAsync_PageLessThanOne_DefaultsToOne(int page)
        {
            SetupPaged(null, 1, 10, 0);

            var result = await _service.GetPagedAsync(null, page, 10, Ct);

            Assert.Equal(1, result.CurrentPage);
            _repo.Verify(
                r => r.GetPagedAsync(null, 1, 10, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPagedAsync_InvalidPageSize_DefaultsToSeven(int pageSize)
        {
            SetupPaged(null, 1, 7, 0);

            var result = await _service.GetPagedAsync(null, 1, pageSize, Ct);

            Assert.Equal(7, result.PageSize);
            _repo.Verify(
                r => r.GetPagedAsync(null, 1, 7, It.IsAny<CancellationToken>()),
                Times.Once);
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
        public async Task GetAllAsync_NoReceptionists_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist>());

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeletedAndMapsFields()
        {
            var active = CreateReceptionist(name: "Sara Mohamed", userId: "user-1");
            var deleted = CreateReceptionist(name: "Deleted Receptionist", isDeleted: true);

            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);

            Assert.Equal(active.Id, item.Id);
            Assert.Equal(active.UserId, item.UserId);
            Assert.Equal(active.Name, item.Name);
            Assert.Equal(active.NationalId, item.NationalId);
            Assert.Equal(active.HireDate, item.HireDate);
            Assert.Equal(active.Salary, item.Salary);
            Assert.Equal(active.CreatedAt, item.CreatedAt);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist>
                {
                    CreateReceptionist(isDeleted: true)
                });

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
            var receptionist = CreateReceptionist(isDeleted: true);
            SetupGetById(receptionist.Id, receptionist);

            Assert.Null(await _service.GetByIdAsync(receptionist.Id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModel()
        {
            var receptionist = CreateReceptionist(name: "Sara Ali");
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.GetByIdAsync(receptionist.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(receptionist.Id, result!.Id);
            Assert.Equal(receptionist.Name, result.Name);
            Assert.Equal(receptionist.NationalId, result.NationalId);
            Assert.Equal(receptionist.HireDate, result.HireDate);
            Assert.Equal(receptionist.Salary, result.Salary);
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
            var receptionist = CreateReceptionist(isDeleted: true);
            SetupGetById(receptionist.Id, receptionist);

            Assert.Null(await _service.GetDetailsAsync(receptionist.Id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModel()
        {
            var receptionist = CreateReceptionist(name: "Sara Ali", userId: "user-1");
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.GetDetailsAsync(receptionist.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(receptionist.Id, result!.Id);
            Assert.Equal(receptionist.UserId, result.UserId);
            Assert.Equal(receptionist.Name, result.Name);
            Assert.Equal(receptionist.NationalId, result.NationalId);
            Assert.Equal(receptionist.HireDate, result.HireDate);
            Assert.Equal(receptionist.Salary, result.Salary);
            Assert.Equal(receptionist.CreatedAt, result.CreatedAt);
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1234567890123")]
        [InlineData("123456789012345")]
        public async Task CreateAsync_InvalidNationalIdLength_ReturnsFailure(string? nationalId)
        {
            var vm = ValidNewVm();
            vm.NationalId = nationalId!;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdLengthMsg, result.Errors);

            _repo.Verify(
                r => r.GetByNationalIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Receptionist>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_NationalIdAlreadyExists_ReturnsFailure()
        {
            SetupGetByNationalId(ValidNationalId, CreateReceptionist());

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Receptionist>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupGetByNationalId(ValidNationalId, null);

            Receptionist? addedReceptionist = null;

            _repo.Setup(r => r.AddAsync(
                    It.IsAny<Receptionist>(),
                    "admin",
                    It.IsAny<CancellationToken>()))
                .Callback<Receptionist, string, CancellationToken>(
                    (receptionist, _, _) => addedReceptionist = receptionist);

            var vm = ValidNewVm();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(addedReceptionist);

            Assert.Equal(addedReceptionist!.Id, result.Data);
            Assert.Equal(vm.Name, addedReceptionist.Name);
            Assert.Equal(vm.NationalId, addedReceptionist.NationalId);
            Assert.Equal(vm.HireDate, addedReceptionist.HireDate);
            Assert.Equal(vm.Salary, addedReceptionist.Salary);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Receptionist>(),
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task CreateAsync_ConstructorOrRepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupGetByNationalId(ValidNationalId, null);

            _repo.Setup(r => r.AddAsync(
                    It.IsAny<Receptionist>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid receptionist"));

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid receptionist", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                  GetByNationalIdAsync
        // =====================================================

        [Fact]
        public async Task GetByNationalIdAsync_Found_ReturnsReceptionist()
        {
            var receptionist = CreateReceptionist();
            SetupGetByNationalId(ValidNationalId, receptionist);

            var result = await _service.GetByNationalIdAsync(ValidNationalId, Ct);

            Assert.Same(receptionist, result);
        }

        [Fact]
        public async Task GetByNationalIdAsync_NotFound_ReturnsNull()
        {
            SetupGetByNationalId(ValidNationalId, null);

            var result = await _service.GetByNationalIdAsync(ValidNationalId, Ct);

            Assert.Null(result);
        }

        // =====================================================
        //                      UpdateAsync
        // =====================================================

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            var vm = ValidUpdateVm(Guid.NewGuid());
            SetupGetById(vm.Id, null);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_Deleted_ReturnsFailure()
        {
            var receptionist = CreateReceptionist(isDeleted: true);
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(receptionist.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1234567890123")]
        [InlineData("123456789012345")]
        public async Task UpdateAsync_InvalidNationalIdLength_ReturnsFailure(string? nationalId)
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);

            var vm = ValidUpdateVm(receptionist.Id);
            vm.NationalId = nationalId!;

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdLengthMsg, result.Errors);

            _repo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<Receptionist>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdBelongsToAnotherReceptionist_ReturnsFailure()
        {
            var receptionist = CreateReceptionist(nationalId: "11111111111111");
            var other = CreateReceptionist(nationalId: ValidNationalId);

            SetupGetById(receptionist.Id, receptionist);
            SetupGetByNationalId(ValidNationalId, other);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(receptionist.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);

            _repo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<Receptionist>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdBelongsToSameReceptionist_Succeeds()
        {
            var receptionist = CreateReceptionist();

            SetupGetById(receptionist.Id, receptionist);
            SetupGetByNationalId(ValidNationalId, receptionist);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(receptionist.Id), "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesReceptionistAndSaves()
        {
            var receptionist = CreateReceptionist(name: "Old Name");
            SetupGetById(receptionist.Id, receptionist);
            SetupGetByNationalId(ValidNationalId, null);

            var vm = ValidUpdateVm(receptionist.Id);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal(vm.Name, receptionist.Name);
            Assert.Equal(vm.NationalId, receptionist.NationalId);
            Assert.Equal(vm.Salary, receptionist.Salary);

            _repo.Verify(
                r => r.UpdateAsync(
                    receptionist,
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);
            SetupGetByNationalId(ValidNationalId, null);

            _repo.Setup(r => r.UpdateAsync(
                    receptionist,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update receptionist"));

            var result = await _service.UpdateAsync(
                ValidUpdateVm(receptionist.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update receptionist", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                    AssignUserAsync
        // =====================================================

        [Fact]
        public async Task AssignUserAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.AssignUserAsync(id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task AssignUserAsync_Deleted_ReturnsFailure()
        {
            var receptionist = CreateReceptionist(isDeleted: true);
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.AssignUserAsync(
                receptionist.Id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task AssignUserAsync_Valid_AssignsAndSaves()
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.AssignUserAsync(
                receptionist.Id, "user-1", "admin", Ct);

            Assert.True(result.Succeeded);

            _repo.Verify(
                r => r.AssignUserAsync(
                    receptionist.Id,
                    "user-1",
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task AssignUserAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);

            _repo.Setup(r => r.AssignUserAsync(
                    receptionist.Id,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("User already assigned"));

            var result = await _service.AssignUserAsync(
                receptionist.Id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("User already assigned", result.Errors);
            VerifyNotSaved();
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

            _repo.Verify(
                r => r.DeleteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeleted_ReturnsFailure()
        {
            var receptionist = CreateReceptionist(isDeleted: true);
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.DeleteAsync(receptionist.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);

            _repo.Verify(
                r => r.DeleteAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesReceptionistAndSavesOnce()
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);

            var result = await _service.DeleteAsync(receptionist.Id, "admin", Ct);

            Assert.True(result.Succeeded);

            _repo.Verify(
                r => r.DeleteAsync(
                    receptionist.Id,
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryThrowsArgumentException_ReturnsFailure()
        {
            var receptionist = CreateReceptionist();
            SetupGetById(receptionist.Id, receptionist);

            _repo.Setup(r => r.DeleteAsync(
                    receptionist.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot delete receptionist"));

            var result = await _service.DeleteAsync(receptionist.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot delete receptionist", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                GetAllUnassignedAsync
        // =====================================================

        [Fact]
        public async Task GetAllUnassignedAsync_ReturnsOnlyActiveReceptionistsWithoutUser()
        {
            var unassigned = CreateReceptionist(name: "Free Receptionist");
            var assigned = CreateReceptionist(name: "Assigned Receptionist", userId: "user-1");
            var deletedUnassigned = CreateReceptionist(
                name: "Deleted Receptionist",
                isDeleted: true);

            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist>
                {
                    unassigned,
                    assigned,
                    deletedUnassigned
                });

            var result = await _service.GetAllUnassignedAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(unassigned.Id, item.Id);
            Assert.Equal(unassigned.Name, item.Name);
        }

        [Fact]
        public async Task GetAllUnassignedAsync_AllAssigned_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist>
                {
                    CreateReceptionist(userId: "user-1")
                });

            Assert.Empty(await _service.GetAllUnassignedAsync(Ct));
        }

        [Fact]
        public async Task GetAllUnassignedAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Receptionist>
                {
                    CreateReceptionist(isDeleted: true)
                });

            Assert.Empty(await _service.GetAllUnassignedAsync(Ct));
        }
    }
}

using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Patient;
using HospitalMS.DAL.Models.PatientModule;
using System.Reflection;

namespace HospitalMS.BLL.Tests.Services.PatientTests
{
    public class PatientServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPatientRepository> _repo = new();
        private readonly PatientService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;

        private const string NotFoundMsg = "Patient not found.";
        private const string NationalIdLengthMsg = "National ID must be exactly 14 digits.";
        private const string NationalIdDuplicateMsg = "National ID already exists and must be unique.";
        private const string ValidNationalId = "12345678901234";

        public PatientServiceTests()
        {
            _uowMock.Setup(u => u.Patients).Returns(_repo.Object);
            _service = new PatientService(_uowMock.Object);
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

        private static Patient CreatePatient(
            string name = "Ahmed Mohamed",
            string nationalId = ValidNationalId,
            Guid? id = null,
            bool isDeleted = false,
            string? userId = null,
            Gender gender = Gender.Male,
            string address = "Main Street",
            string country = "Egypt",
            string city = "Cairo",
            string? notes = "Patient notes")
        {
            // Keep this constructor call aligned with the Patient constructor
            // used by PatientService.CreateAsync.
            var patient = new Patient(
                name,
                nationalId,
                gender,
                address,
                country,
                city,
                notes);

            SetProp(patient, "Id", id ?? Guid.NewGuid());
            SetProp(patient, "IsDeleted", isDeleted);

            if (userId is not null)
                SetProp(patient, "UserId", userId);

            return patient;
        }

        private void SetupGetById(Guid id, Patient? patient) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(patient);

        private void SetupExistsByNationalId(string nationalId, Guid? currentId, bool exists) =>
            _repo.Setup(r => r.ExistsByNationalIdAsync(
                    nationalId, currentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);

        private void SetupPaged(
            string? search,
            int page,
            int pageSize,
            int total,
            params Patient[] items) =>
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

        private static NewPatientViewModel ValidNewVm() => new()
        {
            PatientName = "Ahmed Mohamed",
            NationalId = ValidNationalId,
            Gender = Gender.Male,
            Address = "Main Street",
            Country = "Egypt",
            City = "Cairo",
            Notes = "Patient notes"
        };

        private static UpdatePatientViewModel ValidUpdateVm(Guid id) => new()
        {
            Id = id,
            PatientName = "Updated Patient",
            NationalId = ValidNationalId,
            Gender = Gender.Female,
            Address = "New Street",
            Country = "Egypt",
            City = "Alexandria",
            Notes = "Updated notes"
        };

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var patient = CreatePatient(
                name: "Sara Ali",
                gender: Gender.Female,
                address: "Garden Street",
                country: "Egypt",
                city: "Giza",
                notes: "Follow-up");

            SetupPaged("Sara", 2, 10, 15, patient);

            var result = await _service.GetPagedAsync("Sara", 2, 10, Ct);

            var item = Assert.Single(result.Items);

            Assert.Equal(patient.Id, item.Id);
            Assert.Equal(patient.UserId, item.UserId);
            Assert.Equal(patient.Name, item.PatientName);
            Assert.Equal(patient.NationalId, item.NationalId);
            Assert.Equal(patient.Gender, item.Gender);
            Assert.Equal(patient.Address, item.Address);
            Assert.Equal(patient.Country, item.Country);
            Assert.Equal(patient.City, item.City);
            Assert.Equal(patient.Notes, item.Notes);
            Assert.Equal(patient.CreatedAt, item.CreatedAt);
            Assert.Equal(patient.IsDeleted, item.IsDeleted);

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
        //                      SearchAsync
        // =====================================================

        [Fact]
        public async Task SearchAsync_ReturnsRepositoryResult()
        {
            var patients = new List<Patient>
            {
                CreatePatient("Ahmed"),
                CreatePatient("Ahmed Ali")
            };

            _repo.Setup(r => r.SearchAsync("Ahmed", It.IsAny<CancellationToken>()))
                .ReturnsAsync(patients);

            var result = await _service.SearchAsync("Ahmed", Ct);

            Assert.Equal(patients, result);
            _repo.Verify(
                r => r.SearchAsync("Ahmed", Ct),
                Times.Once);
        }

        [Fact]
        public async Task SearchAsync_NoMatches_ReturnsEmptyList()
        {
            _repo.Setup(r => r.SearchAsync("Nobody", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Patient>());

            var result = await _service.SearchAsync("Nobody", Ct);

            Assert.Empty(result);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoPatients_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Patient>());

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeletedAndMapsFields()
        {
            var active = CreatePatient(
                name: "Ahmed Mohamed",
                userId: "user-1");

            var deleted = CreatePatient(
                name: "Deleted Patient",
                isDeleted: true);

            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Patient> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);

            Assert.Equal(active.Id, item.Id);
            Assert.Equal(active.UserId, item.UserId);
            Assert.Equal(active.Name, item.PatientName);
            Assert.Equal(active.NationalId, item.NationalId);
            Assert.Equal(active.Gender, item.Gender);
            Assert.Equal(active.Address, item.Address);
            Assert.Equal(active.Country, item.Country);
            Assert.Equal(active.City, item.City);
            Assert.Equal(active.Notes, item.Notes);
            Assert.Equal(active.CreatedAt, item.CreatedAt);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Patient>
                {
                    CreatePatient(isDeleted: true)
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
            var patient = CreatePatient(isDeleted: true);
            SetupGetById(patient.Id, patient);

            Assert.Null(await _service.GetByIdAsync(patient.Id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModel()
        {
            var patient = CreatePatient(
                name: "Sara Ali",
                gender: Gender.Female,
                address: "Garden Street",
                city: "Giza");

            SetupGetById(patient.Id, patient);

            var result = await _service.GetByIdAsync(patient.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(patient.Id, result!.Id);
            Assert.Equal(patient.Name, result.PatientName);
            Assert.Equal(patient.Gender, result.Gender);
            Assert.Equal(patient.Address, result.Address);
            Assert.Equal(patient.Country, result.Country);
            Assert.Equal(patient.City, result.City);
            Assert.Equal(patient.Notes, result.Notes);
            Assert.Equal(patient.NationalId, result.NationalId);
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
            var patient = CreatePatient(isDeleted: true);
            SetupGetById(patient.Id, patient);

            Assert.Null(await _service.GetDetailsAsync(patient.Id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModelWithMaskedNationalId()
        {
            var patient = CreatePatient(name: "Sara Ali");
            SetupGetById(patient.Id, patient);

            var result = await _service.GetDetailsAsync(patient.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(patient.Id, result!.Id);
            Assert.Equal(patient.Name, result.PatientName);
            Assert.Equal(patient.Gender, result.Gender);
            Assert.Equal(patient.Address, result.Address);
            Assert.Equal(patient.Country, result.Country);
            Assert.Equal(patient.City, result.City);
            Assert.Equal(patient.Notes, result.Notes);
            Assert.Equal(
                PatientDisplayHelpers.MaskNationalId(patient.NationalId),
                result.NationalId);
            Assert.Equal(patient.CreatedAt, result.CreatedAt);
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
                r => r.ExistsByNationalIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_NationalIdAlreadyExists_ReturnsFailure()
        {
            SetupExistsByNationalId(ValidNationalId, null, true);

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupExistsByNationalId(ValidNationalId, null, false);

            Patient? addedPatient = null;

            _repo.Setup(r => r.AddAsync(
                    It.IsAny<Patient>(),
                    "admin",
                    It.IsAny<CancellationToken>()))
                .Callback<Patient, string, CancellationToken>(
                    (patient, _, _) => addedPatient = patient);

            var vm = ValidNewVm();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(addedPatient);

            Assert.Equal(addedPatient!.Id, result.Data);
            Assert.Equal(vm.PatientName, addedPatient.Name);
            Assert.Equal(vm.NationalId, addedPatient.NationalId);
            Assert.Equal(vm.Gender, addedPatient.Gender);
            Assert.Equal(vm.Address, addedPatient.Address);
            Assert.Equal(vm.Country, addedPatient.Country);
            Assert.Equal(vm.City, addedPatient.City);
            Assert.Equal(vm.Notes, addedPatient.Notes);

            _repo.Verify(
                r => r.AddAsync(
                    It.IsAny<Patient>(),
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupExistsByNationalId(ValidNationalId, null, false);

            _repo.Setup(r => r.AddAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid patient"));

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid patient", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            SetupExistsByNationalId(ValidNationalId, null, false);

            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);
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
            var patient = CreatePatient(isDeleted: true);
            SetupGetById(patient.Id, patient);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(patient.Id), "admin", Ct);

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
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            var vm = ValidUpdateVm(patient.Id);
            vm.NationalId = nationalId!;

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdLengthMsg, result.Errors);

            _repo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdBelongsToAnotherPatient_ReturnsFailure()
        {
            var patient = CreatePatient(nationalId: "11111111111111");
            SetupGetById(patient.Id, patient);
            SetupExistsByNationalId(ValidNationalId, patient.Id, true);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(patient.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);

            _repo.Verify(
                r => r.UpdateAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdIsUnique_Succeeds()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);
            SetupExistsByNationalId(ValidNationalId, patient.Id, false);

            var result = await _service.UpdateAsync(
                ValidUpdateVm(patient.Id), "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesPatientAndSaves()
        {
            var patient = CreatePatient(name: "Old Patient");
            SetupGetById(patient.Id, patient);
            SetupExistsByNationalId(ValidNationalId, patient.Id, false);

            var vm = ValidUpdateVm(patient.Id);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal(vm.PatientName, patient.Name);
            Assert.Equal(vm.Gender, patient.Gender);
            Assert.Equal(vm.Address, patient.Address);
            Assert.Equal(vm.Country, patient.Country);
            Assert.Equal(vm.City, patient.City);
            Assert.Equal(vm.Notes, patient.Notes);

            _repo.Verify(
                r => r.UpdateAsync(patient, "admin", It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);
            SetupExistsByNationalId(ValidNationalId, patient.Id, false);

            _repo.Setup(r => r.UpdateAsync(
                    patient,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update patient"));

            var result = await _service.UpdateAsync(
                ValidUpdateVm(patient.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update patient", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);
            SetupExistsByNationalId(ValidNationalId, patient.Id, false);

            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));

            var result = await _service.UpdateAsync(
                ValidUpdateVm(patient.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);
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
            var patient = CreatePatient(isDeleted: true);
            SetupGetById(patient.Id, patient);

            var result = await _service.AssignUserAsync(
                patient.Id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task AssignUserAsync_Valid_AssignsAndSaves()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            var result = await _service.AssignUserAsync(
                patient.Id, "user-1", "admin", Ct);

            Assert.True(result.Succeeded);

            _repo.Verify(
                r => r.AssignUserAsync(
                    patient.Id,
                    "user-1",
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task AssignUserAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            _repo.Setup(r => r.AssignUserAsync(
                    patient.Id,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid user"));

            var result = await _service.AssignUserAsync(
                patient.Id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid user", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task AssignUserAsync_ThrowsInvalidOperationException_ReturnsFailure()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            _repo.Setup(r => r.AssignUserAsync(
                    patient.Id,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("User is already assigned"));

            var result = await _service.AssignUserAsync(
                patient.Id, "user-1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("User is already assigned", result.Errors);
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
            var patient = CreatePatient(isDeleted: true);
            SetupGetById(patient.Id, patient);

            var result = await _service.DeleteAsync(patient.Id, "admin", Ct);

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
        public async Task DeleteAsync_Valid_DeletesPatientAndSavesOnce()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            var result = await _service.DeleteAsync(patient.Id, "admin", Ct);

            Assert.True(result.Succeeded);

            _repo.Verify(
                r => r.DeleteAsync(
                    patient.Id,
                    "admin",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifySavedOnce();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryThrowsArgumentException_ReturnsFailure()
        {
            var patient = CreatePatient();
            SetupGetById(patient.Id, patient);

            _repo.Setup(r => r.DeleteAsync(
                    patient.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot delete patient"));

            var result = await _service.DeleteAsync(patient.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot delete patient", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                  GetNamesByIdsAsync
        // =====================================================

        [Fact]
        public async Task GetNamesByIdsAsync_ReturnsRepositoryResult()
        {
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var names = new Dictionary<Guid, string>
            {
                [ids[0]] = "Ahmed Mohamed",
                [ids[1]] = "Sara Ali"
            };

            _repo.Setup(r => r.GetNamesByIdsAsync(
                    ids,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(names);

            var result = await _service.GetNamesByIdsAsync(ids, Ct);

            Assert.Equal(names, result);
        }
    }
}

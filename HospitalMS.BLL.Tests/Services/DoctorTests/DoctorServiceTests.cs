using HospitalMS.BLL.ModelVM.Doctor;
using HospitalMS.DAL.Models.DoctorModule;
using System.Reflection;
namespace HospitalMS.BLL.Tests.Services.DoctorTests
{
    public class DoctorServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IDoctorRepository> _repo = new();
        private readonly Mock<IDoctorScheduleRepository> _scheduleRepo = new();
        private readonly DoctorService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;
        private const string NotFoundMsg = "Doctor not found.";
        private const string NationalIdLengthMsg = "National ID must be exactly 14 digits.";
        private const string NationalIdDuplicateMsg = "National ID already exists and must be unique.";
        private const string ValidNationalId = "12345678901234";

        public DoctorServiceTests()
        {
            _uowMock.Setup(u => u.Doctors).Returns(_repo.Object);
            _uowMock.Setup(u => u.DoctorSchedules).Returns(_scheduleRepo.Object);
            _service = new DoctorService(_uowMock.Object);
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

        private static Doctor CreateDoctor(
            string name = "Dr. Ahmed",
            string? userId = null,
            string nationalId = ValidNationalId,
            Guid? id = null,
            bool isDeleted = false,
            string specialtyName = "Cardiology",
            string departmentName = "Surgery")
        {
            var specialtyId = Guid.NewGuid();
            var departmentId = Guid.NewGuid();
            var doctor = new Doctor(
                name, userId, nationalId, "Bio",
                specialtyId, 5, departmentId,
                DateTime.Today.AddYears(-2), 20000m);

            SetProp(doctor, "Id", id ?? Guid.NewGuid());
            SetProp(doctor, "IsDeleted", isDeleted);
            SetProp(doctor, "Specialty", new Specialty(specialtyName));
            SetProp(doctor, "Department", new Department(departmentName));
            return doctor;
        }

        private static DoctorSchedule AddSchedule(     Doctor doctor,     bool isDeleted = false)
        {
            var schedule = new DoctorSchedule(
                days: "Saturday, Sunday",
                startTime: new DateTime(2026, 1, 1, 9, 0, 0),
                endTime: new DateTime(2026, 1, 1, 17, 0, 0),
                doctorId: doctor.Id
            );

            SetProp(schedule, "Id", Guid.NewGuid());
            SetProp(schedule, "IsDeleted", isDeleted);

            var field = typeof(Doctor).GetField(                "_doctorSchedules",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var schedules = (List<DoctorSchedule>)field!.GetValue(doctor)!;

            schedules.Add(schedule);

            return schedule;
        }
        private void SetupGetById(Guid id, Doctor? doctor) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

        private void SetupGetDetails(Guid id, Doctor? doctor) =>
            _repo.Setup(r => r.GetDetailsByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

        private void SetupGetByNationalId(string nationalId, Doctor? doctor) =>
            _repo.Setup(r => r.GetByNationalIdAsync(nationalId, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

        private void SetupPaged(string? search, int page, int pageSize, int total, params Doctor[] items) =>
            _repo.Setup(r => r.GetPagedAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items.ToList(), total));

        private void VerifyNotSaved() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        private void VerifySavedOnce() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        private static NewDoctorViewModel ValidNewVm() => new()
        {
            Name = "Dr. Ahmed",
            UserId = null,
            NationalId = ValidNationalId,
            Bio = "Bio",
            SpecialtyId = Guid.NewGuid(),
            YearsOfExperience = 5,
            DepartmentId = Guid.NewGuid(),
            HireDate = DateTime.Today.AddYears(-1),
            Salary = 20000m
        };

        private static UpdateDoctorViewModel ValidUpdateVm(Guid id) => new()
        {
            Id = id,
            Name = "Dr. Updated",
            NationalId = ValidNationalId,
            Bio = "New bio",
            SpecialtyId = Guid.NewGuid(),
            YearsOfExperience = 8,
            DepartmentId = Guid.NewGuid(),
            HireDate = DateTime.Today.AddYears(-3),
            Salary = 30000m
        };

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var d = CreateDoctor("Dr. Sara", specialtyName: "Neurology", departmentName: "Brain");
            SetupPaged("sa", 2, 10, 15, d);

            var result = await _service.GetPagedAsync("sa", 2, 10, Ct);

            var item = Assert.Single(result.Items);
            Assert.Equal(d.Id, item.Id);
            Assert.Equal(d.UserId, item.UserId);
            Assert.Equal("Dr. Sara", item.Name);
            Assert.Equal(d.Bio, item.Bio);
            Assert.Equal(d.SpecialtyId, item.SpecialtyId);
            Assert.Equal("Neurology", item.SpecialtyName);
            Assert.Equal(d.YearsOfExperience, item.YearsOfExperience);
            Assert.Equal(d.DepartmentId, item.DepartmentId);
            Assert.Equal("Brain", item.DepartmentName);
            Assert.Equal(d.NationalId, item.NationalId);
            Assert.Equal(d.HireDate, item.HireDate);
            Assert.Equal(d.Salary, item.Salary);
            Assert.Equal(d.CreatedAt, item.CreatedAt);
            Assert.False(item.IsDeleted);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(15, result.TotalCount);
            Assert.Equal("sa", result.SearchTerm);
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
        //                    GetDetailsAsync
        // =====================================================

        [Fact]
        public async Task GetDetailsAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetDetails(id, null);

            Assert.Null(await _service.GetDetailsAsync(id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Deleted_ReturnsNull()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetDetails(d.Id, d);

            Assert.Null(await _service.GetDetailsAsync(d.Id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModel()
        {
            var d = CreateDoctor("Dr. Sara", specialtyName: "Neurology", departmentName: "Brain");
            SetupGetDetails(d.Id, d);

            var result = await _service.GetDetailsAsync(d.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(d.Id, result!.Id);
            Assert.Equal("Dr. Sara", result.Name);
            Assert.Equal(d.NationalId, result.NationalId);
            Assert.Equal(d.Bio, result.Bio);
            Assert.Equal("Brain", result.DepartmentName);
            Assert.Equal("Neurology", result.SpecialtyName);
            Assert.Equal(d.YearsOfExperience, result.YearsOfExperience);
            Assert.Equal(d.HireDate, result.HireDate);
            Assert.Equal(d.Salary, result.Salary);
            Assert.Equal(d.CreatedAt, result.CreatedAt);
            Assert.Empty(result.Schedules);
        }

        [Fact]
        public async Task GetDetailsAsync_MapsOnlyActiveSchedules()
        {
            var d = CreateDoctor();

            var active = AddSchedule(d);

            AddSchedule(d, isDeleted: true);

            SetupGetDetails(d.Id, d);

            var result = await _service.GetDetailsAsync(d.Id, Ct);

            var schedule = Assert.Single(result!.Schedules);

            Assert.Equal(active.Id, schedule.Id);
            Assert.Equal(active.Days, schedule.Days);
            Assert.Equal(active.StartTime, schedule.StartTime);
            Assert.Equal(active.EndTime, schedule.EndTime);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoDoctors_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Doctor>());

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeletedAndMapsFields()
        {
            var active = CreateDoctor("Dr. Ahmed", userId: "user1");
            var deleted = CreateDoctor("Dr. Old", isDeleted: true);
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Doctor> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(active.Id, item.Id);
            Assert.Equal("user1", item.UserId);
            Assert.Equal("Dr. Ahmed", item.Name);
            Assert.Equal(active.Bio, item.Bio);
            Assert.Equal(active.SpecialtyId, item.SpecialtyId);
            Assert.Equal(active.YearsOfExperience, item.YearsOfExperience);
            Assert.Equal(active.DepartmentId, item.DepartmentId);
            Assert.Equal(active.NationalId, item.NationalId);
            Assert.Equal(active.HireDate, item.HireDate);
            Assert.Equal(active.Salary, item.Salary);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Doctor> { CreateDoctor(isDeleted: true) });

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
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            Assert.Null(await _service.GetByIdAsync(d.Id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModelWithActiveSchedules()
        {
            var d = CreateDoctor("Dr. Sara", departmentName: "Brain");
            var active = AddSchedule(d);
            AddSchedule(d, isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.GetByIdAsync(d.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(d.Id, result!.Id);
            Assert.Equal("Dr. Sara", result.Name);
            Assert.Equal(d.Bio, result.Bio);
            Assert.Equal(d.SpecialtyId, result.SpecialtyId);
            Assert.Equal(d.YearsOfExperience, result.YearsOfExperience);
            Assert.Equal(d.DepartmentId, result.DepartmentId);
            Assert.Equal(d.HireDate, result.HireDate);
            Assert.Equal(d.Salary, result.Salary);
            Assert.Equal(d.NationalId, result.NationalId);
            Assert.Equal("Brain", result.DepartmentName);
            Assert.Equal(active.Id, Assert.Single(result.Schedules).Id);
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Theory]
        [InlineData("specialty", "Specialty is required.")]
        [InlineData("department", "Department is required.")]
        [InlineData("years", "Years of experience is required.")]
        [InlineData("hire", "Hire date is required.")]
        [InlineData("salary", "Salary is required.")]
        public async Task CreateAsync_MissingRequiredField_ReturnsFailureWithoutCheckingNationalId(string missing, string expected)
        {
            var vm = ValidNewVm();
            switch (missing)
            {
                case "specialty": vm.SpecialtyId = null; break;
                case "department": vm.DepartmentId = null; break;
                case "years": vm.YearsOfExperience = null; break;
                case "hire": vm.HireDate = null; break;
                case "salary": vm.Salary = null; break;
            }

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(expected, result.Errors);
            _repo.Verify(r => r.GetByNationalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _repo.Verify(r => r.AddAsync(It.IsAny<Doctor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1234567890123")]    // 13
        [InlineData("123456789012345")]  // 15
        public async Task CreateAsync_InvalidNationalIdLength_ReturnsFailure(string? nationalId)
        {
            var vm = ValidNewVm();
            vm.NationalId = nationalId!;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdLengthMsg, result.Errors);
            _repo.Verify(r => r.GetByNationalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_NationalIdAlreadyExists_ReturnsFailure()
        {
            SetupGetByNationalId(ValidNationalId, CreateDoctor());
            var vm = ValidNewVm();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);
            _repo.Verify(r => r.AddAsync(It.IsAny<Doctor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupGetByNationalId(ValidNationalId, null);
            Doctor? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<Doctor>(), "admin", It.IsAny<CancellationToken>()))
                .Callback<Doctor, string, CancellationToken>((d, _, _) => added = d);
            var vm = ValidNewVm();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(added);
            Assert.Equal(added!.Id, result.Data);
            Assert.Equal(vm.Name, added.Name);
            Assert.Equal(vm.NationalId, added.NationalId);
            Assert.Equal(vm.SpecialtyId, added.SpecialtyId);
            Assert.Equal(vm.DepartmentId, added.DepartmentId);
            Assert.Equal(vm.YearsOfExperience, added.YearsOfExperience);
            Assert.Equal(vm.Salary, added.Salary);
            VerifySavedOnce();
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupGetByNationalId(ValidNationalId, null);
            _repo.Setup(r => r.AddAsync(It.IsAny<Doctor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid doctor"));

            var result = await _service.CreateAsync(ValidNewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid doctor", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            SetupGetByNationalId(ValidNationalId, null);
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
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.UpdateAsync(ValidUpdateVm(d.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Theory]
        [InlineData("specialty", "Specialty is required.")]
        [InlineData("department", "Department is required.")]
        [InlineData("years", "Years of experience is required.")]
        [InlineData("hire", "Hire date is required.")]
        [InlineData("salary", "Salary is required.")]
        public async Task UpdateAsync_MissingRequiredField_ReturnsFailure(string missing, string expected)
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            var vm = ValidUpdateVm(d.Id);
            switch (missing)
            {
                case "specialty": vm.SpecialtyId = null; break;
                case "department": vm.DepartmentId = null; break;
                case "years": vm.YearsOfExperience = null; break;
                case "hire": vm.HireDate = null; break;
                case "salary": vm.Salary = null; break;
            }

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(expected, result.Errors);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Doctor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("123456789012345")]
        public async Task UpdateAsync_InvalidNationalIdLength_ReturnsFailure(string? nationalId)
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            var vm = ValidUpdateVm(d.Id);
            vm.NationalId = nationalId!;

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdLengthMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdBelongsToAnotherDoctor_ReturnsFailure()
        {
            var d = CreateDoctor();
            var other = CreateDoctor(nationalId: ValidNationalId);
            SetupGetById(d.Id, d);
            SetupGetByNationalId(ValidNationalId, other);

            var result = await _service.UpdateAsync(ValidUpdateVm(d.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Doctor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NationalIdBelongsToSameDoctor_Succeeds()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            SetupGetByNationalId(ValidNationalId, d);

            var result = await _service.UpdateAsync(ValidUpdateVm(d.Id), "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesDoctorAndSaves()
        {
            var d = CreateDoctor("Dr. Old");
            SetupGetById(d.Id, d);
            SetupGetByNationalId(ValidNationalId, null);
            var vm = ValidUpdateVm(d.Id);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal("Dr. Updated", d.Name);
            Assert.Equal(vm.SpecialtyId, d.SpecialtyId);
            Assert.Equal(vm.DepartmentId, d.DepartmentId);
            Assert.Equal(vm.YearsOfExperience, d.YearsOfExperience);
            Assert.Equal(vm.Salary, d.Salary);
            _repo.Verify(r => r.UpdateAsync(d, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            SetupGetByNationalId(ValidNationalId, null);
            _repo.Setup(r => r.UpdateAsync(d, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update"));

            var result = await _service.UpdateAsync(ValidUpdateVm(d.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_SaveThrowsDbUpdateException_ReturnsDuplicateMessage()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            SetupGetByNationalId(ValidNationalId, null);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique constraint"));

            var result = await _service.UpdateAsync(ValidUpdateVm(d.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NationalIdDuplicateMsg, result.Errors);
        }

        // =====================================================
        //                    ChangeSalaryAsync
        // =====================================================

        [Fact]
        public async Task ChangeSalaryAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.ChangeSalaryAsync(id, 1000m, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task ChangeSalaryAsync_Deleted_ReturnsFailure()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.ChangeSalaryAsync(d.Id, 1000m, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task ChangeSalaryAsync_Valid_ChangesAndSaves()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);

            var result = await _service.ChangeSalaryAsync(d.Id, 35000m, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.ChangeSalaryAsync(d.Id, 35000m, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task ChangeSalaryAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.ChangeSalaryAsync(d.Id, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid salary"));

            var result = await _service.ChangeSalaryAsync(d.Id, -1m, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid salary", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                  ChangeDepartmentAsync
        // =====================================================

        [Fact]
        public async Task ChangeDepartmentAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.ChangeDepartmentAsync(id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task ChangeDepartmentAsync_Deleted_ReturnsFailure()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.ChangeDepartmentAsync(d.Id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task ChangeDepartmentAsync_Valid_ChangesAndSaves()
        {
            var d = CreateDoctor();
            var departmentId = Guid.NewGuid();
            SetupGetById(d.Id, d);

            var result = await _service.ChangeDepartmentAsync(d.Id, departmentId, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.ChangeDepartmentAsync(d.Id, departmentId, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task ChangeDepartmentAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.ChangeDepartmentAsync(d.Id, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid department"));

            var result = await _service.ChangeDepartmentAsync(d.Id, Guid.Empty, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid department", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                  ChangeSpecialtyAsync
        // =====================================================

        [Fact]
        public async Task ChangeSpecialtyAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(id, null);

            var result = await _service.ChangeSpecialtyAsync(id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task ChangeSpecialtyAsync_Deleted_ReturnsFailure()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.ChangeSpecialtyAsync(d.Id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task ChangeSpecialtyAsync_Valid_ChangesAndSaves()
        {
            var d = CreateDoctor();
            var specialtyId = Guid.NewGuid();
            SetupGetById(d.Id, d);

            var result = await _service.ChangeSpecialtyAsync(d.Id, specialtyId, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.ChangeSpecialtyAsync(d.Id, specialtyId, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task ChangeSpecialtyAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.ChangeSpecialtyAsync(d.Id, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid specialty"));

            var result = await _service.ChangeSpecialtyAsync(d.Id, Guid.Empty, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid specialty", result.Errors);
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

            var result = await _service.AssignUserAsync(id, "user1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task AssignUserAsync_Deleted_ReturnsFailure()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.AssignUserAsync(d.Id, "user1", "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task AssignUserAsync_Valid_AssignsAndSaves()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);

            var result = await _service.AssignUserAsync(d.Id, "user1", "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.AssignUserAsync(d.Id, "user1", "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task AssignUserAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.AssignUserAsync(d.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("User already assigned"));

            var result = await _service.AssignUserAsync(d.Id, "user1", "admin", Ct);

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
            _scheduleRepo.Verify(s => s.DeleteByDoctorIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeleted_ReturnsFailure()
        {
            var d = CreateDoctor(isDeleted: true);
            SetupGetById(d.Id, d);

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesSchedulesAndDoctorThenSavesOnce()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _scheduleRepo.Verify(s => s.DeleteByDoctorIdAsync(d.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.DeleteAsync(d.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task DeleteAsync_DoctorDeleteThrowsArgumentException_ReturnsFailureWithoutSaving()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _repo.Setup(r => r.DeleteAsync(d.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Doctor has appointments"));

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Doctor has appointments", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_ScheduleDeleteThrowsArgumentException_DoesNotDeleteDoctor()
        {
            var d = CreateDoctor();
            SetupGetById(d.Id, d);
            _scheduleRepo.Setup(s => s.DeleteByDoctorIdAsync(d.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Schedule error"));

            var result = await _service.DeleteAsync(d.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Schedule error", result.Errors);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // =====================================================
        //                GetAllUnassignedAsync
        // =====================================================

        [Fact]
        public async Task GetAllUnassignedAsync_ReturnsOnlyActiveDoctorsWithoutUser()
        {
            var unassigned = CreateDoctor("Dr. Free", userId: null);
            var assigned = CreateDoctor("Dr. Assigned", userId: "user1");
            var deletedUnassigned = CreateDoctor("Dr. Deleted", userId: null, isDeleted: true);
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Doctor> { unassigned, assigned, deletedUnassigned });

            var result = await _service.GetAllUnassignedAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(unassigned.Id, item.Id);
            Assert.Equal("Dr. Free", item.Name);
        }

        [Fact]
        public async Task GetAllUnassignedAsync_AllAssigned_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Doctor> { CreateDoctor(userId: "user1") });

            Assert.Empty(await _service.GetAllUnassignedAsync(Ct));
        }

        // =====================================================
        //                  GetNamesByIdsAsync
        // =====================================================

        [Fact]
        public async Task GetNamesByIdsAsync_ReturnsRepositoryResult()
        {
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var names = new Dictionary<Guid, string> { [ids[0]] = "Dr. A", [ids[1]] = "Dr. B" };
            _repo.Setup(r => r.GetNamesByIdsAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(names);

            var result = await _service.GetNamesByIdsAsync(ids, Ct);

            Assert.Equal(names, result);
        }

        // =====================================================
        //                   GetByUserIdAsync
        // =====================================================

        [Fact]
        public async Task GetByUserIdAsync_DoctorFound_ReturnsId()
        {
            var d = CreateDoctor(userId: "user1");
            _repo.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(d);

            Assert.Equal(d.Id, await _service.GetByUserIdAsync("user1", Ct));
        }

        [Fact]
        public async Task GetByUserIdAsync_NoDoctor_ReturnsNull()
        {
            _repo.Setup(r => r.GetByUserIdAsync("nobody", It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

            Assert.Null(await _service.GetByUserIdAsync("nobody", Ct));
        }
    }
}
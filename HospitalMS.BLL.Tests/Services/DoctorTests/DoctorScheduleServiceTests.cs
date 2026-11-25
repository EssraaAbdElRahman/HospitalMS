using HospitalMS.BLL.ModelVM.DoctorSchedule;
using HospitalMS.DAL.Models.DoctorModule;
using System.Reflection;

namespace HospitalMS.BLL.Tests.Services.DoctorTests
{
    public class DoctorScheduleServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IDoctorScheduleRepository> _repo = new();
        private readonly DoctorScheduleService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;
        private const string NotFoundMsg = "DoctorSchedule not found.";
        private const string ConflictMsg = "Doctor already has a schedule during the selected days and time.";

        private static readonly DateTime Start = new(2026, 1, 1, 9, 0, 0);
        private static readonly DateTime End = new(2026, 1, 1, 17, 0, 0);

        public DoctorScheduleServiceTests()
        {
            _uowMock.Setup(u => u.DoctorSchedules).Returns(_repo.Object);
            _service = new DoctorScheduleService(_uowMock.Object);
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

        private static DoctorSchedule CreateSchedule(
            string days = "Sunday, Monday",
            Guid? doctorId = null,
            Guid? id = null,
            bool isDeleted = false,
            string doctorName = "Dr. Ahmed",
            bool withDoctor = true)
        {
            var s = new DoctorSchedule(days, Start, End, doctorId ?? Guid.NewGuid());
            SetProp(s, "Id", id ?? Guid.NewGuid());
            SetProp(s, "IsDeleted", isDeleted);
            if (withDoctor)
            {
                var doctor = (Doctor)Activator.CreateInstance(typeof(Doctor), nonPublic: true)!;
                SetProp(doctor, "Name", doctorName);
                SetProp(s, "Doctor", doctor);
            }
            return s;
        }

        private void SetupGetById(Guid id, DoctorSchedule? s) =>
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(s);

        private void SetupGetDetails(Guid id, DoctorSchedule? s) =>
            _repo.Setup(r => r.GetDetailsByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(s);

        private void SetupPaged(string? search, int page, int pageSize, int total, params DoctorSchedule[] items) =>
            _repo.Setup(r => r.GetPagedAsync(search, page, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync((items.ToList(), total));

        private void SetupConflict(bool value) =>
            _repo.Setup(r => r.HasConflictAsync(
                    It.IsAny<List<Days>>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);

        private void VerifyNotSaved() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        private void VerifySavedOnce() =>
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        private static NewDoctorScheduleViewModel NewVm() => new()
        {
            DoctorId = Guid.NewGuid(),
            Days = new List<Days> { Days.Sunday, Days.Monday },
            StartTime = Start,
            EndTime = End
        };

        private static UpdateDoctorScheduleViewModel UpdateVm(Guid id) => new()
        {
            Id = id,
            DoctorId = Guid.NewGuid(),
            Days = new List<Days> { Days.Tuesday },
            StartTime = Start,
            EndTime = End
        };

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        [Fact]
        public async Task GetPagedAsync_ReturnsMappedItemsAndPagingInfo()
        {
            var s = CreateSchedule(doctorName: "Dr. Sara");
            SetupPaged("sa", 2, 10, 15, s);

            var result = await _service.GetPagedAsync("sa", 2, 10, Ct);

            var item = Assert.Single(result.Items);
            Assert.Equal(s.Id, item.Id);
            Assert.Equal(s.Days, item.Days);
            Assert.Equal(s.StartTime, item.StartTime);
            Assert.Equal(s.EndTime, item.EndTime);
            Assert.Equal(s.DoctorId, item.DoctorId);
            Assert.Equal("Dr. Sara", item.DoctorName);
            Assert.Equal(s.CreatedAt, item.CreatedAt);
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
            var s = CreateSchedule(isDeleted: true);
            SetupGetDetails(s.Id, s);

            Assert.Null(await _service.GetDetailsAsync(s.Id, Ct));
        }

        [Fact]
        public async Task GetDetailsAsync_Exists_ReturnsMappedViewModel()
        {
            var s = CreateSchedule(doctorName: "Dr. Sara");
            SetupGetDetails(s.Id, s);

            var result = await _service.GetDetailsAsync(s.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(s.Id, result!.Id);
            Assert.Equal(s.Days, result.Days);
            Assert.Equal(s.StartTime, result.StartTime);
            Assert.Equal(s.EndTime, result.EndTime);
            Assert.Equal(s.DoctorId, result.DoctorId);
            Assert.Equal("Dr. Sara", result.DoctorName);
            Assert.Equal(s.CreatedAt, result.CreatedAt);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoSchedules_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DoctorSchedule>());

            Assert.Empty(await _service.GetAllAsync(Ct));
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeletedAndMapsFields()
        {
            var active = CreateSchedule(doctorName: "Dr. Ahmed");
            var deleted = CreateSchedule(isDeleted: true);
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DoctorSchedule> { active, deleted });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(active.Id, item.Id);
            Assert.Equal(active.Days, item.Days);
            Assert.Equal(active.DoctorId, item.DoctorId);
            Assert.Equal("Dr. Ahmed", item.DoctorName);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DoctorSchedule> { CreateSchedule(isDeleted: true) });

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
            var s = CreateSchedule(isDeleted: true, withDoctor: false);
            SetupGetById(s.Id, s);

            Assert.Null(await _service.GetByIdAsync(s.Id, Ct));
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ParsesDaysStringIntoEnumList()
        {
            var s = CreateSchedule("Sunday, Monday", withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.GetByIdAsync(s.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(s.Id, result!.Id);
            Assert.Equal(s.DoctorId, result.DoctorId);
            Assert.Equal(new[] { Days.Sunday, Days.Monday }, result.Days);
            Assert.Equal(s.StartTime, result.StartTime);
            Assert.Equal(s.EndTime, result.EndTime);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidDayNames_AreIgnored()
        {
            var s = CreateSchedule("Sunday, Funday, , Tuesday", withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.GetByIdAsync(s.Id, Ct);

            Assert.Equal(new[] { Days.Sunday, Days.Tuesday }, result!.Days);
        }

        [Fact]
        public async Task GetByIdAsync_SingleDay_ReturnsOneItem()
        {
            var s = CreateSchedule("Monday", withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.GetByIdAsync(s.Id, Ct);

            Assert.Equal(new[] { Days.Monday }, result!.Days);
        }

        // =====================================================
        //                  GetByDoctorIdAsync
        // =====================================================

        [Fact]
        public async Task GetByDoctorIdAsync_ExcludesDeletedAndMapsFields()
        {
            var doctorId = Guid.NewGuid();
            var active = CreateSchedule(doctorId: doctorId, withDoctor: false);
            var deleted = CreateSchedule(doctorId: doctorId, isDeleted: true, withDoctor: false);
            _repo.Setup(r => r.GetByDoctorIdAsync(doctorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DoctorSchedule> { active, deleted });

            var result = await _service.GetByDoctorIdAsync(doctorId, Ct);

            var item = Assert.Single(result);
            Assert.Equal(active.Id, item.Id);
            Assert.Equal(doctorId, item.DoctorId);
            Assert.Equal(active.Days, item.Days);
            Assert.Equal(active.StartTime, item.StartTime);
            Assert.Equal(active.EndTime, item.EndTime);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetByDoctorIdAsync_NoSchedules_ReturnsEmpty()
        {
            var doctorId = Guid.NewGuid();
            _repo.Setup(r => r.GetByDoctorIdAsync(doctorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DoctorSchedule>());

            Assert.Empty(await _service.GetByDoctorIdAsync(doctorId, Ct));
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Fact]
        public async Task CreateAsync_EmptyDoctorId_ReturnsFailure()
        {
            var vm = NewVm();
            vm.DoctorId = Guid.Empty;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Doctor is required.", result.Errors);
            _repo.Verify(r => r.AddAsync(It.IsAny<DoctorSchedule>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_NoDays_ReturnsFailure()
        {
            var vm = NewVm();
            vm.Days = new List<Days>();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("At least one day is required.", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_NullDays_ReturnsFailure()
        {
            var vm = NewVm();
            vm.Days = null!;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("At least one day is required.", result.Errors);
        }

        [Fact]
        public async Task CreateAsync_DefaultStartTime_ReturnsFailure()
        {
            var vm = NewVm();
            vm.StartTime = default;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Start time is required.", result.Errors);
        }

        [Fact]
        public async Task CreateAsync_DefaultEndTime_ReturnsFailure()
        {
            var vm = NewVm();
            vm.EndTime = default;

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("End time is required.", result.Errors);
        }

        [Theory]
        [InlineData(9, 9)]    
        [InlineData(10, 9)]   
        public async Task CreateAsync_EndNotAfterStart_ReturnsFailure(int startHour, int endHour)
        {
            var vm = NewVm();
            vm.StartTime = new DateTime(2026, 1, 1, startHour, 0, 0);
            vm.EndTime = new DateTime(2026, 1, 1, endHour, 0, 0);

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("End time must be after start time.", result.Errors);
            _repo.Verify(r => r.HasConflictAsync(
                It.IsAny<List<Days>>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Conflict_ReturnsFailureWithoutAdding()
        {
            SetupConflict(true);

            var result = await _service.CreateAsync(NewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _repo.Verify(r => r.AddAsync(It.IsAny<DoctorSchedule>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsId()
        {
            SetupConflict(false);
            DoctorSchedule? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<DoctorSchedule>(), "admin", It.IsAny<CancellationToken>()))
                .Callback<DoctorSchedule, string, CancellationToken>((s, _, _) => added = s);
            var vm = NewVm();

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(added);
            Assert.Equal(added!.Id, result.Data);
            Assert.Equal("Sunday, Monday", added.Days);
            Assert.Equal(vm.DoctorId, added.DoctorId);
            Assert.Equal(vm.StartTime, added.StartTime);
            Assert.Equal(vm.EndTime, added.EndTime);
            VerifySavedOnce();
        }

        [Fact]
        public async Task CreateAsync_DuplicateDays_AreDistinctBeforeConflictCheckAndSave()
        {
            SetupConflict(false);
            DoctorSchedule? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<DoctorSchedule>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<DoctorSchedule, string, CancellationToken>((s, _, _) => added = s);
            var vm = NewVm();
            vm.Days = new List<Days> { Days.Monday, Days.Monday, Days.Sunday };

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal("Monday, Sunday", added!.Days);
            _repo.Verify(r => r.HasConflictAsync(
                It.Is<List<Days>>(l => l.Count == 2),
                vm.DoctorId, vm.StartTime, vm.EndTime, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            SetupConflict(false);
            _repo.Setup(r => r.AddAsync(It.IsAny<DoctorSchedule>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid schedule"));

            var result = await _service.CreateAsync(NewVm(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid schedule", result.Errors);
            VerifyNotSaved();
        }

        // =====================================================
        //                      UpdateAsync
        // =====================================================

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            var vm = UpdateVm(Guid.NewGuid());
            SetupGetById(vm.Id, null);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_Deleted_ReturnsFailure()
        {
            var s = CreateSchedule(isDeleted: true, withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.UpdateAsync(UpdateVm(s.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_EmptyDoctorId_ReturnsValidationFailure()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            var vm = UpdateVm(s.Id);
            vm.DoctorId = Guid.Empty;

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Doctor is required.", result.Errors);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_NoDays_ReturnsValidationFailure()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            var vm = UpdateVm(s.Id);
            vm.Days = new List<Days>();

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("At least one day is required.", result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_EndNotAfterStart_ReturnsValidationFailure()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            var vm = UpdateVm(s.Id);
            vm.StartTime = new DateTime(2026, 1, 1, 12, 0, 0);
            vm.EndTime = new DateTime(2026, 1, 1, 11, 0, 0);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("End time must be after start time.", result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_Conflict_ReturnsFailureAndExcludesCurrentSchedule()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            SetupConflict(true);
            var vm = UpdateVm(s.Id);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _repo.Verify(r => r.HasConflictAsync(
                It.IsAny<List<Days>>(), vm.DoctorId, vm.StartTime, vm.EndTime,
                s.Id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<DoctorSchedule>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesAndSaves()
        {
            var s = CreateSchedule("Sunday", withDoctor: false);
            SetupGetById(s.Id, s);
            SetupConflict(false);
            var vm = UpdateVm(s.Id);
            vm.Days = new List<Days> { Days.Tuesday, Days.Tuesday, Days.Monday };

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.Equal("Tuesday, Monday", s.Days);
            Assert.Equal(vm.DoctorId, s.DoctorId);
            _repo.Verify(r => r.UpdateAsync(s, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailureMessage()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            SetupConflict(false);
            _repo.Setup(r => r.UpdateAsync(s, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot update"));

            var result = await _service.UpdateAsync(UpdateVm(s.Id), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot update", result.Errors);
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
            VerifyNotSaved();
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeleted_ReturnsFailure()
        {
            var s = CreateSchedule(isDeleted: true, withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesAndSaves()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _repo.Verify(r => r.DeleteAsync(s.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            VerifySavedOnce();
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var s = CreateSchedule(withDoctor: false);
            SetupGetById(s.Id, s);
            _repo.Setup(r => r.DeleteAsync(s.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Schedule in use"));

            var result = await _service.DeleteAsync(s.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Schedule in use", result.Errors);
            VerifyNotSaved();
        }
    }
}
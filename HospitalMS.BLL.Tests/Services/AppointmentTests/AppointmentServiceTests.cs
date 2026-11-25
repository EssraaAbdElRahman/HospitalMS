using HospitalMS.BLL.ModelVM.Appointment;
using HospitalMS.DAL.Models.DoctorModule;
using HospitalMS.DAL.Models.PatientModule;
using System.Reflection;

namespace HospitalMS.BLL.Tests.Services.AppointmentTests
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IAppointmentRepository> _apptRepo = new();
        private readonly Mock<IPatientRepository> _patientRepo = new();
        private readonly Mock<IDoctorRepository> _doctorRepo = new();
        private readonly AppointmentService _service;

        private static readonly CancellationToken Ct = CancellationToken.None;
        private static readonly DateTime Tomorrow = DateTime.Today.AddDays(1);
        private static readonly DateTime Slot = Tomorrow.AddHours(10);

        public AppointmentServiceTests()
        {
            _uowMock.Setup(u => u.Appointments).Returns(_apptRepo.Object);
            _uowMock.Setup(u => u.Patients).Returns(_patientRepo.Object);
            _uowMock.Setup(u => u.Doctors).Returns(_doctorRepo.Object);
            _service = new AppointmentService(_uowMock.Object);
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

        private static Appointment CreateAppointment(
            Guid? id = null,
            Guid? patientId = null,
            Guid? doctorId = null,
            DateTime? date = null,
            AppointmentStatus status = AppointmentStatus.Scheduled,
            bool isDeleted = false)
        {
            var type = new NewAppointmentViewModel().AppointmentType;  
            var a = new Appointment(
                patientId ?? Guid.NewGuid(),
                doctorId ?? Guid.NewGuid(),
                date ?? Slot,
                type);

            SetProp(a, "Id", id ?? Guid.NewGuid());
            SetProp(a, "Status", status);
            SetProp(a, "IsDeleted", isDeleted);
            return a;
        }

        private void SetupNames(
            Dictionary<Guid, string>? patients = null,
            Dictionary<Guid, string>? doctors = null)
        {
            _patientRepo.Setup(p => p.GetNamesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(patients ?? new Dictionary<Guid, string>());
            _doctorRepo.Setup(d => d.GetNamesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doctors ?? new Dictionary<Guid, string>());
        }

        private void SetupGetById(Appointment? appointment, Guid id) =>
            _apptRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(appointment);

        private void SetupGetByDate(DateTime date, params Appointment[] items) =>
            _apptRepo.Setup(r => r.GetByDateAsync(date.Date, It.IsAny<CancellationToken>()))
                .ReturnsAsync(items.ToList());

        private void VerifyNoWrites()
        {
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static NewAppointmentViewModel NewVm(Guid? doctorId = null) => new()
        {
            PatientId = Guid.NewGuid(),
            DoctorId = doctorId ?? Guid.NewGuid(),
            AppointmentDate = Tomorrow,
            AppointmentTime = TimeSpan.FromHours(10)
        };

        private const string ConflictMsg = "This doctor already has an appointment at the selected time.";
        private const string NotFoundMsg = "Appointment not found.";

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoAppointments_ReturnsEmptyAndSkipsNameLookups()
        {
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment>());

            var result = await _service.GetAllAsync(Ct);

            Assert.Empty(result);
            _patientRepo.Verify(p => p.GetNamesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            _doctorRepo.Verify(d => d.GetNamesByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_ExcludesDeletedAppointments()
        {
            var active = CreateAppointment();
            var deleted = CreateAppointment(isDeleted: true);
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment> { active, deleted });
            SetupNames();

            var result = await _service.GetAllAsync(Ct);

            Assert.Single(result);
            Assert.Equal(active.Id, result[0].Id);
        }

        [Fact]
        public async Task GetAllAsync_OnlyDeleted_ReturnsEmpty()
        {
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment> { CreateAppointment(isDeleted: true) });

            var result = await _service.GetAllAsync(Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_MapsAllFieldsAndNames()
        {
            var a = CreateAppointment(status: AppointmentStatus.Check_in);
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment> { a });
            SetupNames(
                new Dictionary<Guid, string> { [a.PatientId] = "Sara" },
                new Dictionary<Guid, string> { [a.DoctorId] = "Dr. Ahmed" });

            var result = await _service.GetAllAsync(Ct);

            var item = Assert.Single(result);
            Assert.Equal(a.Id, item.Id);
            Assert.Equal(a.PatientId, item.PatientId);
            Assert.Equal("Sara", item.PatientName);
            Assert.Equal(a.DoctorId, item.DoctorId);
            Assert.Equal("Dr. Ahmed", item.DoctorName);
            Assert.Equal(a.AppointmentDate, item.AppointmentDate);
            Assert.Equal(AppointmentStatus.Check_in, item.Status);
            Assert.Equal(a.AppointmentType, item.AppointmentType);
            Assert.False(item.IsDeleted);
        }

        [Fact]
        public async Task GetAllAsync_MissingNames_FallBackToUnknown()
        {
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment> { CreateAppointment() });
            SetupNames(); 

            var result = await _service.GetAllAsync(Ct);

            Assert.Equal("Unknown", result[0].PatientName);
            Assert.Equal("Unknown", result[0].DoctorName);
        }

        [Fact]
        public async Task GetAllAsync_SamePatientAndDoctor_RequestsDistinctIds()
        {
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            _apptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment>
                {
                    CreateAppointment(patientId: patientId, doctorId: doctorId),
                    CreateAppointment(patientId: patientId, doctorId: doctorId)
                });
            SetupNames();

            await _service.GetAllAsync(Ct);

            _patientRepo.Verify(p => p.GetNamesByIdsAsync(
                It.Is<List<Guid>>(l => l.Count == 1 && l[0] == patientId),
                It.IsAny<CancellationToken>()), Times.Once);
            _doctorRepo.Verify(d => d.GetNamesByIdsAsync(
                It.Is<List<Guid>>(l => l.Count == 1 && l[0] == doctorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // =====================================================
        //                     GetByDateAsync
        // =====================================================

        [Fact]
        public async Task GetByDateAsync_ReturnsMappedItems()
        {
            var a = CreateAppointment();
            SetupGetByDate(Tomorrow, a);
            SetupNames(new Dictionary<Guid, string> { [a.PatientId] = "Sara" });

            var result = await _service.GetByDateAsync(Tomorrow, Ct);

            Assert.Single(result);
            Assert.Equal("Sara", result[0].PatientName);
        }

        [Fact]
        public async Task GetByDateAsync_Empty_ReturnsEmpty()
        {
            SetupGetByDate(Tomorrow);

            var result = await _service.GetByDateAsync(Tomorrow, Ct);

            Assert.Empty(result);
        }

        // =====================================================
        //                   GetByPatientIdAsync
        // =====================================================

        [Fact]
        public async Task GetByPatientIdAsync_ExcludesDeleted()
        {
            var patientId = Guid.NewGuid();
            var active = CreateAppointment(patientId: patientId);
            var deleted = CreateAppointment(patientId: patientId, isDeleted: true);
            _apptRepo.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment> { active, deleted });
            SetupNames();

            var result = await _service.GetByPatientIdAsync(patientId, Ct);

            Assert.Single(result);
            Assert.Equal(active.Id, result[0].Id);
        }

        [Fact]
        public async Task GetByPatientIdAsync_NoAppointments_ReturnsEmpty()
        {
            var patientId = Guid.NewGuid();
            _apptRepo.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment>());

            var result = await _service.GetByPatientIdAsync(patientId, Ct);

            Assert.Empty(result);
        }

        // =====================================================
        //                      GetByIdAsync
        // =====================================================

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.GetByIdAsync(id, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_Deleted_ReturnsNull()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.GetByIdAsync(a.Id, Ct);

            Assert.Null(result);
            _patientRepo.Verify(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsMappedViewModel()
        {
            var a = CreateAppointment(date: Slot, status: AppointmentStatus.Scheduled);
            SetupGetById(a, a.Id);

            var patient = (Patient)Activator.CreateInstance(typeof(Patient), nonPublic: true)!;
            SetProp(patient, "Name", "Sara");
            var departmentId = Guid.NewGuid();
            var doctor = (Doctor)Activator.CreateInstance(typeof(Doctor), nonPublic: true)!;
            SetProp(doctor, "DepartmentId", departmentId);

            _patientRepo.Setup(p => p.GetByIdAsync(a.PatientId, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
            _doctorRepo.Setup(d => d.GetByIdAsync(a.DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

            var result = await _service.GetByIdAsync(a.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal(a.Id, result!.Id);
            Assert.Equal(a.PatientId, result.PatientId);
            Assert.Equal(a.DoctorId, result.DoctorId);
            Assert.Equal(Slot, result.AppointmentDate);
            Assert.Equal(Slot.TimeOfDay, result.AppointmentTime);
            Assert.Equal(AppointmentStatus.Scheduled, result.Status);
            Assert.Equal("Sara", result.PatientName);
            Assert.Equal(departmentId, result.DepartmentId);
        }

        [Fact]
        public async Task GetByIdAsync_PatientAndDoctorMissing_UsesDefaults()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            _patientRepo.Setup(p => p.GetByIdAsync(a.PatientId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
            _doctorRepo.Setup(d => d.GetByIdAsync(a.DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

            var result = await _service.GetByIdAsync(a.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal("Unknown", result!.PatientName);
            Assert.Equal(Guid.Empty, result.DepartmentId);
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Fact]
        public async Task CreateAsync_DoctorHasConflict_ReturnsFailureWithoutSaving()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow, CreateAppointment(doctorId: vm.DoctorId, date: Slot));

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _apptRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoWrites();
        }

        [Fact]
        public async Task CreateAsync_SameTimeDifferentDoctor_Succeeds()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow, CreateAppointment(doctorId: Guid.NewGuid(), date: Slot));

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateAsync_SameDoctorDifferentTime_Succeeds()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow, CreateAppointment(doctorId: vm.DoctorId, date: Slot.AddHours(1)));

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateAsync_ConflictingAppointmentDeleted_Succeeds()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow, CreateAppointment(doctorId: vm.DoctorId, date: Slot, isDeleted: true));

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsSavesAndReturnsNewId()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow);
            Appointment? added = null;
            _apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), "admin", It.IsAny<CancellationToken>()))
                .Callback<Appointment, string, CancellationToken>((a, _, _) => added = a);

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            Assert.NotNull(added);
            Assert.Equal(added!.Id, result.Data);
            Assert.Equal(vm.PatientId, added.PatientId);
            Assert.Equal(vm.DoctorId, added.DoctorId);
            Assert.Equal(Slot, added.AppointmentDate);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AddThrowsArgumentException_ReturnsFailureMessage()
        {
            var vm = NewVm();
            SetupGetByDate(Tomorrow);
            _apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid appointment"));

            var result = await _service.CreateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid appointment", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                      UpdateAsync
        // =====================================================

        private UpdateAppointmentViewModel UpdateVm(Appointment a, Guid? doctorId = null,
            AppointmentStatus status = AppointmentStatus.Completed) => new()
            {
                Id = a.Id,
                DoctorId = doctorId ?? a.DoctorId,
                AppointmentDate = Tomorrow,
                AppointmentTime = TimeSpan.FromHours(14),
                Status = status
            };

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            var vm = new UpdateAppointmentViewModel { Id = Guid.NewGuid() };
            SetupGetById(null, vm.Id);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNoWrites();
        }

        [Fact]
        public async Task UpdateAsync_Deleted_ReturnsFailure()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.UpdateAsync(UpdateVm(a), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task UpdateAsync_Conflict_ReturnsFailureWithoutChanges()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            var newTime = Tomorrow.AddHours(14);
            SetupGetByDate(Tomorrow, CreateAppointment(doctorId: a.DoctorId, date: newTime));

            var result = await _service.UpdateAsync(UpdateVm(a), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _apptRepo.Verify(r => r.RescheduleAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoWrites();
        }

        [Fact]
        public async Task UpdateAsync_OnlyConflictIsItself_IsExcludedAndSucceeds()
        {
            var a = CreateAppointment(date: Tomorrow.AddHours(14));
            SetupGetById(a, a.Id);
            SetupGetByDate(Tomorrow, a);
            var result = await _service.UpdateAsync(UpdateVm(a), "admin", Ct);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateAsync_Valid_ReschedulesUpdatesStatusChangesDoctorAndSaves()
        {
            var a = CreateAppointment();
            var newDoctor = Guid.NewGuid();
            SetupGetById(a, a.Id);
            SetupGetByDate(Tomorrow);
            var vm = UpdateVm(a, newDoctor, AppointmentStatus.Completed);

            var result = await _service.UpdateAsync(vm, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.RescheduleAsync(a.Id, Tomorrow.AddHours(14), "admin", It.IsAny<CancellationToken>()), Times.Once);
            _apptRepo.Verify(r => r.UpdateStatusAsync(a.Id, AppointmentStatus.Completed, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _apptRepo.Verify(r => r.ChangeDoctorAsync(a.Id, newDoctor, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrowsArgumentException_ReturnsFailure()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            SetupGetByDate(Tomorrow);
            _apptRepo.Setup(r => r.UpdateStatusAsync(a.Id, It.IsAny<AppointmentStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid status transition"));

            var result = await _service.UpdateAsync(UpdateVm(a), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid status transition", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                    ChangeDoctorAsync
        // =====================================================

        [Fact]
        public async Task ChangeDoctorAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.ChangeDoctorAsync(id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task ChangeDoctorAsync_Deleted_ReturnsFailure()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.ChangeDoctorAsync(a.Id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task ChangeDoctorAsync_NewDoctorBusy_ReturnsConflict()
        {
            var a = CreateAppointment();
            var newDoctor = Guid.NewGuid();
            SetupGetById(a, a.Id);
            SetupGetByDate(a.AppointmentDate, CreateAppointment(doctorId: newDoctor, date: a.AppointmentDate));

            var result = await _service.ChangeDoctorAsync(a.Id, newDoctor, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _apptRepo.Verify(r => r.ChangeDoctorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoWrites();
        }

        [Fact]
        public async Task ChangeDoctorAsync_Valid_ChangesAndSaves()
        {
            var a = CreateAppointment();
            var newDoctor = Guid.NewGuid();
            SetupGetById(a, a.Id);
            SetupGetByDate(a.AppointmentDate);

            var result = await _service.ChangeDoctorAsync(a.Id, newDoctor, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.ChangeDoctorAsync(a.Id, newDoctor, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeDoctorAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            SetupGetByDate(a.AppointmentDate);
            _apptRepo.Setup(r => r.ChangeDoctorAsync(a.Id, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Doctor not available"));

            var result = await _service.ChangeDoctorAsync(a.Id, Guid.NewGuid(), "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Doctor not available", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                    UpdateStatusAsync
        // =====================================================

        [Fact]
        public async Task UpdateStatusAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.UpdateStatusAsync(id, AppointmentStatus.Completed, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNoWrites();
        }

        [Fact]
        public async Task UpdateStatusAsync_Deleted_ReturnsFailure()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.UpdateStatusAsync(a.Id, AppointmentStatus.Completed, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Theory]
        [InlineData(AppointmentStatus.Check_in)]
        [InlineData(AppointmentStatus.Completed)]
        [InlineData(AppointmentStatus.Canceled)]
        [InlineData(AppointmentStatus.No_show)]
        public async Task UpdateStatusAsync_Valid_UpdatesAndSaves(AppointmentStatus status)
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);

            var result = await _service.UpdateStatusAsync(a.Id, status, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.UpdateStatusAsync(a.Id, status, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            _apptRepo.Setup(r => r.UpdateStatusAsync(a.Id, It.IsAny<AppointmentStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot change status"));

            var result = await _service.UpdateStatusAsync(a.Id, AppointmentStatus.Completed, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot change status", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                     RescheduleAsync
        // =====================================================

        [Fact]
        public async Task RescheduleAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.RescheduleAsync(id, Slot, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task RescheduleAsync_Deleted_ReturnsFailure()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.RescheduleAsync(a.Id, Slot, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task RescheduleAsync_Conflict_ReturnsFailure()
        {
            var a = CreateAppointment();
            var newSlot = Tomorrow.AddHours(15);
            SetupGetById(a, a.Id);
            SetupGetByDate(newSlot, CreateAppointment(doctorId: a.DoctorId, date: newSlot));

            var result = await _service.RescheduleAsync(a.Id, newSlot, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(ConflictMsg, result.Errors);
            _apptRepo.Verify(r => r.RescheduleAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoWrites();
        }

        [Fact]
        public async Task RescheduleAsync_Valid_ReschedulesAndSaves()
        {
            var a = CreateAppointment();
            var newSlot = Tomorrow.AddHours(15);
            SetupGetById(a, a.Id);
            SetupGetByDate(newSlot);

            var result = await _service.RescheduleAsync(a.Id, newSlot, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.RescheduleAsync(a.Id, newSlot, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RescheduleAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var a = CreateAppointment();
            var newSlot = Tomorrow.AddHours(15);
            SetupGetById(a, a.Id);
            SetupGetByDate(newSlot);
            _apptRepo.Setup(r => r.RescheduleAsync(a.Id, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Date in the past"));

            var result = await _service.RescheduleAsync(a.Id, newSlot, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Date in the past", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                      DeleteAsync
        // =====================================================

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.DeleteAsync(id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            VerifyNoWrites();
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeleted_ReturnsFailure()
        {
            var a = CreateAppointment(isDeleted: true);
            SetupGetById(a, a.Id);

            var result = await _service.DeleteAsync(a.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
            _apptRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_DeletesAndSaves()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);

            var result = await _service.DeleteAsync(a.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.DeleteAsync(a.Id, "admin", It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_ReturnsFailure()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);
            _apptRepo.Setup(r => r.DeleteAsync(a.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Cannot delete completed appointment"));

            var result = await _service.DeleteAsync(a.Id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains("Cannot delete completed appointment", result.Errors);
            VerifyNoWrites();
        }

        // =====================================================
        //                    GetFilteredAsync
        // =====================================================

        private (Appointment a1, Appointment a2, Appointment a3) SeedFilterData(Guid doctor1, Guid doctor2)
        {
            var a1 = CreateAppointment(doctorId: doctor1, date: Tomorrow.AddHours(12), status: AppointmentStatus.Scheduled);
            var a2 = CreateAppointment(doctorId: doctor1, date: Tomorrow.AddHours(9), status: AppointmentStatus.Check_in);
            var a3 = CreateAppointment(doctorId: doctor2, date: Tomorrow.AddHours(11), status: AppointmentStatus.Completed);
            SetupGetByDate(Tomorrow, a1, a2, a3);
            SetupNames(
                new Dictionary<Guid, string>
                {
                    [a1.PatientId] = "Sara Ali",
                    [a2.PatientId] = "Omar Hassan",
                    [a3.PatientId] = "Mona Sara"
                });
            return (a1, a2, a3);
        }

        [Fact]
        public async Task GetFilteredAsync_NoFilters_ReturnsAllOrderedByDateWithCounts()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, null, null, null, Ct);

            Assert.Equal(3, result.TotalItems);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(result.Items.OrderBy(i => i.AppointmentDate).Select(i => i.Id),
                         result.Items.Select(i => i.Id));
            Assert.Equal(1, result.ScheduledCount);
            Assert.Equal(1, result.CheckedInCount);
            Assert.Equal(1, result.CompletedCount);
        }

        [Fact]
        public async Task GetFilteredAsync_ByDoctor_ReturnsOnlyThatDoctor()
        {
            var d1 = Guid.NewGuid();
            SeedFilterData(d1, Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, d1, null, null, Ct);

            Assert.Equal(2, result.TotalItems);
            Assert.All(result.Items, i => Assert.Equal(d1, i.DoctorId));
        }

        [Fact]
        public async Task GetFilteredAsync_EmptyGuidDoctor_IsIgnored()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, Guid.Empty, null, null, Ct);

            Assert.Equal(3, result.TotalItems);
        }

        [Fact]
        public async Task GetFilteredAsync_ByStatus_ReturnsOnlyThatStatus()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, null, AppointmentStatus.Completed, null, Ct);

            Assert.Single(result.Items);
            Assert.Equal(AppointmentStatus.Completed, result.Items[0].Status);
            Assert.Equal(1, result.TotalItems);
            Assert.Equal(0, result.ScheduledCount);
            Assert.Equal(1, result.CompletedCount);
        }

        [Fact]
        public async Task GetFilteredAsync_BySearchTerm_IsCaseInsensitiveContains()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, null, null, "sara", Ct);

            Assert.Equal(2, result.TotalItems); // "Sara Ali" و "Mona Sara"
            Assert.All(result.Items, i => Assert.Contains("sara", i.PatientName, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetFilteredAsync_BlankSearchTerm_IsIgnored(string? term)
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, null, null, term, Ct);

            Assert.Equal(3, result.TotalItems);
        }

        [Fact]
        public async Task GetFilteredAsync_CombinedFilters_AppliesAll()
        {
            var d1 = Guid.NewGuid();
            SeedFilterData(d1, Guid.NewGuid());

            var result = await _service.GetFilteredAsync(
                Tomorrow, d1, AppointmentStatus.Scheduled, "ali", Ct);

            Assert.Single(result.Items);
            Assert.Equal("Sara Ali", result.Items[0].PatientName);
        }

        [Fact]
        public async Task GetFilteredAsync_NoMatches_ReturnsZeroCounts()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow, null, null, "zzz", Ct);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(0, result.ScheduledCount);
            Assert.Equal(0, result.CheckedInCount);
            Assert.Equal(0, result.CompletedCount);
        }

        [Fact]
        public async Task GetFilteredAsync_DateWithTime_IsTruncatedToDate()
        {
            SeedFilterData(Guid.NewGuid(), Guid.NewGuid());

            var result = await _service.GetFilteredAsync(Tomorrow.AddHours(15), null, null, null, Ct);

            Assert.Equal(3, result.TotalItems);
        }

        // =====================================================
        //                 NoShowAsync / CancelAsync
        // =====================================================

        [Fact]
        public async Task NoShowAsync_SetsNoShowStatus()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);

            var result = await _service.NoShowAsync(a.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.UpdateStatusAsync(a.Id, AppointmentStatus.No_show, "admin", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_SetsCanceledStatus()
        {
            var a = CreateAppointment();
            SetupGetById(a, a.Id);

            var result = await _service.CancelAsync(a.Id, "admin", Ct);

            Assert.True(result.Succeeded);
            _apptRepo.Verify(r => r.UpdateStatusAsync(a.Id, AppointmentStatus.Canceled, "admin", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NoShowAsync_AppointmentNotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.NoShowAsync(id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }

        [Fact]
        public async Task CancelAsync_AppointmentNotFound_ReturnsFailure()
        {
            var id = Guid.NewGuid();
            SetupGetById(null, id);

            var result = await _service.CancelAsync(id, "admin", Ct);

            Assert.False(result.Succeeded);
            Assert.Contains(NotFoundMsg, result.Errors);
        }
    }
}
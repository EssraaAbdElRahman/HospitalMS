using HospitalMS.BLL.Authorization;
using HospitalMS.DAL.Models.AppModule;
using HospitalMS.DAL.Models.AppointmentModule;
using HospitalMS.DAL.Models.DoctorModule;
using HospitalMS.DAL.Models.PatientModule;
using HospitalMS.DAL.Models.ReceptionistModule;
using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Seeding
{
    public sealed class DemoDataSeeder : IDemoDataSeeder
    {
        private const string DemoActor = "demo-seed";
        private const string DemoPassword = "HospitalDemo!2026";

        private static readonly string[] DepartmentNames =
        [
            "Cardiology",
            "Emergency Medicine",
            "Orthopedics",
            "Pediatrics"
        ];

        private static readonly string[] SpecialtyNames =
        [
            "Cardiology",
            "Emergency Medicine",
            "Interventional Cardiology",
            "Neonatology",
            "Orthopedic Surgery",
            "Pediatrics"
        ];

        private static readonly DoctorSeed[] Doctors =
        [
            new("Avery Bennett", "90000000000001", 1, 1, 18, 245000m),
            new("Jordan Hayes", "90000000000002", 1, 1, 7, 198000m),
            new("Morgan Chen", "90000000000003", 0, 0, 22, 275000m),
            new("Riley Patel", "90000000000004", 2, 0, 11, 232000m),
            new("Casey Morgan", "90000000000005", 5, 3, 15, 221000m),
            new("Taylor Reed", "90000000000006", 3, 3, 9, 205000m),
            new("Cameron Brooks", "90000000000007", 4, 2, 19, 254000m),
            new("Quinn Foster", "90000000000008", 4, 2, 5, 187000m)
        ];

        private static readonly ReceptionistSeed[] Receptionists =
        [
            new("Sam Rivera", "91000000000001"),
            new("Alex Kim", "91000000000002"),
            new("Jamie Ellis", "91000000000003")
        ];

        private static readonly PatientSeed[] Patients =
        [
            new("Robin Ellis", "92000000000001", Gender.Female),
            new("Drew Parker", "92000000000002", Gender.Male),
            new("Skyler James", "92000000000003", Gender.Female),
            new("Emerson Blake", "92000000000004", Gender.Male),
            new("Harper Lane", "92000000000005", Gender.Female),
            new("Logan Quinn", "92000000000006", Gender.Male),
            new("Sage Turner", "92000000000007", Gender.Female),
            new("Peyton Clark", "92000000000008", Gender.Male),
            new("Kendall Ross", "92000000000009", Gender.Female),
            new("Finley Ward", "92000000000010", Gender.Male),
            new("Remy Stone", "92000000000011", Gender.Female),
            new("Dakota Gray", "92000000000012", Gender.Male),
            new("Arden Wells", "92000000000013", Gender.Female),
            new("Micah Cole", "92000000000014", Gender.Male),
            new("Shiloh Dean", "92000000000015", Gender.Female)
        ];

        private static readonly AppointmentSeed[] AppointmentPlan =
        [
            new(-7, AppointmentStatus.Completed, AppointmentStatus.Completed),
            new(-6, AppointmentStatus.Completed, AppointmentStatus.Completed),
            new(-5, AppointmentStatus.Completed, AppointmentStatus.Completed),
            new(-4, AppointmentStatus.Canceled, AppointmentStatus.Canceled),
            new(-3, AppointmentStatus.Canceled, AppointmentStatus.No_show),
            new(-2, AppointmentStatus.No_show, AppointmentStatus.No_show),
            new(-1, AppointmentStatus.Completed, AppointmentStatus.Completed),
            new(0, AppointmentStatus.Scheduled, AppointmentStatus.Check_in),
            new(0, AppointmentStatus.Scheduled, AppointmentStatus.Completed),
            new(1, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled),
            new(1, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled),
            new(2, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled),
            new(2, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled),
            new(3, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled),
            new(4, AppointmentStatus.Scheduled, AppointmentStatus.Scheduled)
        ];

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DemoDataSeeder(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                await EnsureUserAsync(
                    "HospitalMS Demo Administrator", "admin@demo.hospital.test",
                    UserType.Admin, null, "SuperAdmin", cancellationToken);

                var departmentByName = await EnsureDepartmentsAsync(cancellationToken);
                var specialtyByName = await EnsureSpecialtiesAsync(cancellationToken);

                for (var index = 0; index < Doctors.Length; index++)
                {
                    var seed = Doctors[index];
                    var doctorUser = await EnsureUserAsync(
                        seed.Name, $"doctor{index + 1}@demo.hospital.test",
                        UserType.Staff, StaffType.Doctor, "Doctor", cancellationToken);

                    var existingDoctors = await _unitOfWork.Doctors.GetAllAsync(cancellationToken);
                    var existingDoctor = existingDoctors.FirstOrDefault(x => x.NationalId == seed.NationalId);
                    if (existingDoctor is null)
                    {
                        var doctor = new Doctor(
                            seed.Name,
                            doctorUser.Id,
                            seed.NationalId,
                            $"Fictional demo profile for {seed.Name}, serving patients in {departmentByName[DepartmentNames[seed.DepartmentIndex]].Name}.",
                            specialtyByName[SpecialtyNames[seed.SpecialtyIndex]].Id,
                            seed.YearsOfExperience,
                            departmentByName[DepartmentNames[seed.DepartmentIndex]].Id,
                            DateTime.Today.AddYears(-(seed.YearsOfExperience + 1)),
                            seed.Salary);

                        await _unitOfWork.Doctors.AddAsync(doctor, DemoActor, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    else if (existingDoctor.UserId is null)
                    {
                        await _unitOfWork.Doctors.AssignUserAsync(
                            existingDoctor.Id, doctorUser.Id, DemoActor, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    else if (existingDoctor.UserId != doctorUser.Id)
                    {
                        throw new InvalidOperationException(
                            $"Demo doctor National ID {seed.NationalId} is linked to a different account.");
                    }
                }

                var receptionistUsers = new List<ApplicationUser>(Receptionists.Length);
                for (var index = 0; index < Receptionists.Length; index++)
                {
                    var seed = Receptionists[index];
                    receptionistUsers.Add(await EnsureUserAsync(
                        seed.Name, $"receptionist{index + 1}@demo.hospital.test",
                        UserType.Staff, StaffType.Receptionist, "Receptionist", cancellationToken));
                }

                var patientUsers = new List<ApplicationUser>(2);
                for (var index = 0; index < 2; index++)
                {
                    var patient = Patients[index];
                    patientUsers.Add(await EnsureUserAsync(
                        patient.Name, $"patient{index + 1}@demo.hospital.test",
                        UserType.Patient, null, "Patient", cancellationToken));
                }

                await EnsureReceptionistsAsync(receptionistUsers, cancellationToken);
                var patientsByNationalId = await EnsurePatientsAsync(patientUsers, cancellationToken);

                var doctorsByNationalId = (await _unitOfWork.Doctors.GetAllAsync(cancellationToken))
                    .Where(x => Doctors.Any(seed => seed.NationalId == x.NationalId))
                    .ToDictionary(x => x.NationalId);
                foreach (var seed in Doctors)
                {
                    var doctor = doctorsByNationalId[seed.NationalId];
                    var schedules = await _unitOfWork.DoctorSchedules.GetByDoctorIdAsync(doctor.Id, cancellationToken);
                    await EnsureSchedulesAsync(doctor, schedules, cancellationToken);
                }

                await EnsureAppointmentsAsync(
                    doctorsByNationalId, patientsByNationalId, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        private async Task<Dictionary<string, Department>> EnsureDepartmentsAsync(CancellationToken ct)
        {
            var existing = await _unitOfWork.Departments.GetAllAsync(ct);
            var result = existing.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in DepartmentNames)
            {
                if (result.ContainsKey(name))
                    continue;

                var department = new Department(name);
                await _unitOfWork.Departments.AddAsync(department, DemoActor, ct);
                result.Add(name, department);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return result;
        }

        private async Task<Dictionary<string, Specialty>> EnsureSpecialtiesAsync(CancellationToken ct)
        {
            var existing = await _unitOfWork.Specialties.GetAllAsync(ct);
            var result = existing.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var name in SpecialtyNames)
            {
                if (result.ContainsKey(name))
                    continue;

                var specialty = new Specialty(name);
                await _unitOfWork.Specialties.AddAsync(specialty, DemoActor, ct);
                result.Add(name, specialty);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return result;
        }

        private async Task EnsureReceptionistsAsync(
            IReadOnlyList<ApplicationUser> users,
            CancellationToken ct)
        {
            var existing = await _unitOfWork.Receptionists.GetAllAsync(ct);
            for (var index = 0; index < Receptionists.Length; index++)
            {
                var seed = Receptionists[index];
                var receptionist = existing.FirstOrDefault(x => x.NationalId == seed.NationalId);
                if (receptionist is null)
                {
                    receptionist = new Receptionist(
                        seed.Name, seed.NationalId, DateTime.Today.AddYears(-3 - index), 52000m + index * 2500m);
                    await _unitOfWork.Receptionists.AddAsync(receptionist, DemoActor, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                    existing.Add(receptionist);
                }

                if (receptionist.UserId is null)
                {
                    await _unitOfWork.Receptionists.AssignUserAsync(
                        receptionist.Id, users[index].Id, DemoActor, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                else if (receptionist.UserId != users[index].Id)
                {
                    throw new InvalidOperationException(
                        $"Demo receptionist National ID {seed.NationalId} is linked to a different account.");
                }
            }
        }

        private async Task<Dictionary<string, Patient>> EnsurePatientsAsync(
            IReadOnlyList<ApplicationUser> users,
            CancellationToken ct)
        {
            var existing = await _unitOfWork.Patients.GetAllAsync(ct);
            var result = new Dictionary<string, Patient>(StringComparer.Ordinal);

            for (var index = 0; index < Patients.Length; index++)
            {
                var seed = Patients[index];
                var patient = existing.FirstOrDefault(x => x.NationalId == seed.NationalId);
                if (patient is null)
                {
                    patient = new Patient(
                        seed.Name,
                        seed.NationalId,
                        seed.Gender,
                        $"{100 + index} Demo Avenue",
                        "United States",
                        "Riverton",
                        "Fictional demonstration record.");
                    await _unitOfWork.Patients.AddAsync(patient, DemoActor, ct);
                    existing.Add(patient);
                }

                if (index < users.Count && patient.UserId is null)
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                    await _unitOfWork.Patients.AssignUserAsync(
                        patient.Id, users[index].Id, DemoActor, ct);
                }
                else if (index < users.Count && patient.UserId != users[index].Id)
                {
                    throw new InvalidOperationException(
                        $"Demo patient National ID {seed.NationalId} is linked to a different account.");
                }

                result.Add(seed.NationalId, patient);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return result;
        }

        private async Task EnsureSchedulesAsync(
            Doctor doctor,
            List<DoctorSchedule> existing,
            CancellationToken ct)
        {
            var days = string.Join(", ", Enum.GetNames<Days>());
            var schedules = new[]
            {
                (Start: TimeSpan.FromHours(9), End: TimeSpan.FromHours(12)),
                (Start: TimeSpan.FromHours(13), End: TimeSpan.FromHours(16))
            };

            foreach (var schedule in schedules)
            {
                if (existing.Any(x => x.Days == days
                    && x.StartTime.TimeOfDay == schedule.Start
                    && x.EndTime.TimeOfDay == schedule.End))
                {
                    continue;
                }

                var entity = new DoctorSchedule(
                    days,
                    DateTime.Today.Add(schedule.Start),
                    DateTime.Today.Add(schedule.End),
                    doctor.Id);
                await _unitOfWork.DoctorSchedules.AddAsync(entity, DemoActor, ct);
                existing.Add(entity);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        private async Task EnsureAppointmentsAsync(
            IReadOnlyDictionary<string, Doctor> doctorsByNationalId,
            IReadOnlyDictionary<string, Patient> patientsByNationalId,
            CancellationToken ct)
        {
            var expected = new List<Appointment>(30);
            for (var group = 0; group < AppointmentPlan.Length; group++)
            {
                var plan = AppointmentPlan[group];
                var appointmentTime = TimeSpan.FromHours(9 + (group / 8) * 0.5);
                var date = DateTime.Today.AddDays(plan.DayOffset).Add(appointmentTime);
                var doctorIndexes = new[] { group % Doctors.Length, (group + 4) % Doctors.Length };
                var statuses = new[] { plan.FirstStatus, plan.SecondStatus };

                for (var index = 0; index < 2; index++)
                {
                    var doctorSeed = Doctors[doctorIndexes[index]];
                    var patientSeed = Patients[(group * 2 + index) % Patients.Length];
                    var appointment = new Appointment(
                        patientsByNationalId[patientSeed.NationalId].Id,
                        doctorsByNationalId[doctorSeed.NationalId].Id,
                        date.AddMinutes(index * 30),
                        index % 2 == 0 ? AppointmentType.Consultation : AppointmentType.FollowUp);
                    appointment.UpdateStatus(statuses[index]);
                    expected.Add(appointment);
                }
            }

            var existingDemoAppointments = (await _unitOfWork.Appointments.GetAllAsync(ct))
                .Where(x => x.CreatedBy == DemoActor)
                .ToList();

            if (MatchesAppointments(existingDemoAppointments, expected))
                return;

            foreach (var appointment in existingDemoAppointments)
                await _unitOfWork.Appointments.DeleteAsync(appointment.Id, DemoActor, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            foreach (var appointment in expected)
                await _unitOfWork.Appointments.AddAsync(appointment, DemoActor, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static bool MatchesAppointments(
            IReadOnlyCollection<Appointment> existing,
            IReadOnlyCollection<Appointment> expected)
        {
            if (existing.Count != expected.Count)
                return false;

            return expected.All(item => existing.Any(current =>
                current.PatientId == item.PatientId
                && current.DoctorId == item.DoctorId
                && current.AppointmentDate == item.AppointmentDate
                && current.Status == item.Status
                && current.AppointmentType == item.AppointmentType));
        }

        private async Task<ApplicationUser> EnsureUserAsync(
            string fullName,
            string email,
            UserType userType,
            StaffType? staffType,
            string roleName,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = userType == UserType.Patient
                    ? ApplicationUser.CreatePatientUser(fullName, email, DemoActor)
                    : ApplicationUser.CreateStaff(fullName, email, userType, staffType, DemoActor);

                user.EmailConfirmed = true;
                user.Activate(DemoActor);
                if (userType == UserType.Admin)
                    user.PromoteToAdmin(DemoActor);

                var createResult = await _userManager.CreateAsync(user, DemoPassword);
                EnsureIdentitySuccess(createResult, $"create demo account '{email}'");
            }
            else if (user.UserType != userType || user.StaffType != staffType)
            {
                throw new InvalidOperationException(
                    $"The existing account '{email}' does not match the expected demo account type.");
            }
            else
            {
                if (user.Status != AccountStatus.Active)
                    user.Activate(DemoActor);
                if (!user.EmailConfirmed)
                    user.EmailConfirmed = true;
                if (userType == UserType.Admin && !user.IsAdmin)
                    user.PromoteToAdmin(DemoActor);

                var updateResult = await _userManager.UpdateAsync(user);
                EnsureIdentitySuccess(updateResult, $"activate demo account '{email}'");
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                EnsureIdentitySuccess(roleResult, $"assign role '{roleName}' to '{email}'");
            }

            return user;
        }

        private static void EnsureIdentitySuccess(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Could not {operation}: {errors}");
            }
        }

        private sealed record DoctorSeed(
            string Name, string NationalId, int SpecialtyIndex, int DepartmentIndex,
            int YearsOfExperience, decimal Salary);

        private sealed record ReceptionistSeed(string Name, string NationalId);
        private sealed record PatientSeed(string Name, string NationalId, Gender Gender);
        private sealed record AppointmentSeed(
            int DayOffset, AppointmentStatus FirstStatus, AppointmentStatus SecondStatus);
    }
}

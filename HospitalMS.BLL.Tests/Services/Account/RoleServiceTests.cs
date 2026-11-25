using HospitalMS.BLL.Helper;
using HospitalMS.BLL.ModelVM.Role;
using HospitalMS.DAL.Database;
using Microsoft.AspNetCore.Identity;
using MockQueryable;
using System.Security.Claims;

namespace HospitalMS.BLL.Tests.Services.Role
{
    public class RoleServiceTests : IDisposable
    {
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HospitalDbContext _context;
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                null, null, null, null);

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _context = CreateContext();

            _service = new RoleService(
                _roleManagerMock.Object,
                _context,
                _userManagerMock.Object);
        }

        public void Dispose() => _context.Dispose();

        // ---------------- Helpers ----------------

        private static HospitalDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<HospitalDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new HospitalDbContext(options);
        }

        private static ApplicationRole CreateRole(
            string id = "role1",
            string name = "Doctor",
            string? description = "desc")
        {
            var role = new ApplicationRole(name, description);
            role.Id = id;
            return role;
        }

        private static ApplicationUser CreateUser(string id, string fullName, string email)
        {
            var user = ApplicationUser.CreateStaff(fullName, email, UserType.Staff, StaffType.Doctor, "admin1");
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Id))!.SetValue(user, id);
            return user;
        }

        private void SetupRoles(params ApplicationRole[] roles) =>
            _roleManagerMock.Setup(r => r.Roles).Returns(roles.ToList().BuildMock());

        private void SeedUserRoles(params (string UserId, string RoleId)[] links)
        {
            foreach (var (userId, roleId) in links)
                _context.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = roleId });
            _context.SaveChanges();
        }

        // =====================================================
        //                      CreateAsync
        // =====================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_EmptyRoleName_ReturnsRequiredError(string? name)
        {
            var vm = new CreateRoleViewModel { RoleName = name!, RoleDescription = "d" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Role name is required.", result.Errors);
            _roleManagerMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
            _roleManagerMock.Verify(r => r.RoleExistsAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("SuperAdmin")]
        [InlineData("superadmin")]
        [InlineData("SUPERADMIN")]
        public async Task CreateAsync_ProtectedRoleName_ReturnsReservedError(string name)
        {
            var vm = new CreateRoleViewModel { RoleName = name, RoleDescription = "d" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("This role name is reserved and cannot be used.", result.Errors);
            _roleManagerMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_RoleAlreadyExists_ReturnsExistsError()
        {
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Nurse")).ReturnsAsync(true);
            var vm = new CreateRoleViewModel { RoleName = "Nurse", RoleDescription = "d" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("A role with this name already exists.", result.Errors);
            _roleManagerMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ProtectedAndExisting_ReturnsBothErrors()
        {
            _roleManagerMock.Setup(r => r.RoleExistsAsync("SuperAdmin")).ReturnsAsync(true);
            var vm = new CreateRoleViewModel { RoleName = "SuperAdmin", RoleDescription = "d" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(2, result.Errors.Count());
            Assert.Contains("This role name is reserved and cannot be used.", result.Errors);
            Assert.Contains("A role with this name already exists.", result.Errors);
        }

        [Fact]
        public async Task CreateAsync_ValidRole_CreatesRoleAndReturnsSuccess()
        {
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Nurse")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);
            var vm = new CreateRoleViewModel { RoleName = "Nurse", RoleDescription = "Nurses role" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.CreateAsync(
                It.Is<ApplicationRole>(x => x.Name == "Nurse" && x.Description == "Nurses role")),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_IdentityCreateFails_ReturnsIdentityErrors()
        {
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Nurse")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = "Create failed" },
                    new IdentityError { Description = "Another error" }));
            var vm = new CreateRoleViewModel { RoleName = "Nurse", RoleDescription = "d" };

            var result = await _service.CreateAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Create failed", result.Errors);
            Assert.Contains("Another error", result.Errors);
        }

        // =====================================================
        //                      GetAllAsync
        // =====================================================

        [Fact]
        public async Task GetAllAsync_NoRoles_ReturnsEmptyList()
        {
            SetupRoles();

            var result = await _service.GetAllAsync(CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsRolesOrderedByName()
        {
            SetupRoles(
                CreateRole("r2", "Receptionist"),
                CreateRole("r1", "Doctor"),
                CreateRole("r3", "Admin"));

            var result = await _service.GetAllAsync(CancellationToken.None);

            Assert.Equal(new[] { "Admin", "Doctor", "Receptionist" }, result.Select(r => r.Name));
        }

        [Fact]
        public async Task GetAllAsync_MapsUserCountsPerRole()
        {
            SetupRoles(CreateRole("r1", "Doctor"), CreateRole("r2", "Receptionist"));
            SeedUserRoles(("u1", "r1"), ("u2", "r1"), ("u3", "r2"));

            var result = await _service.GetAllAsync(CancellationToken.None);

            Assert.Equal(2, result.Single(r => r.Id == "r1").UserCount);
            Assert.Equal(1, result.Single(r => r.Id == "r2").UserCount);
        }

        [Fact]
        public async Task GetAllAsync_RoleWithoutUsers_HasZeroUserCount()
        {
            SetupRoles(CreateRole("r1", "Doctor"));

            var result = await _service.GetAllAsync(CancellationToken.None);

            Assert.Equal(0, result.Single().UserCount);
        }

        [Fact]
        public async Task GetAllAsync_MapsDescriptionAndSystemRoleFlag()
        {
            SetupRoles(
                CreateRole("r1", "SuperAdmin", "System owner"),
                CreateRole("r2", "Doctor", "Doctors"));

            var result = await _service.GetAllAsync(CancellationToken.None);

            var superAdmin = result.Single(r => r.Name == "SuperAdmin");
            var doctor = result.Single(r => r.Name == "Doctor");

            Assert.True(superAdmin.IsSystemRole);
            Assert.Equal("System owner", superAdmin.Description);
            Assert.False(doctor.IsSystemRole);
            Assert.Equal("Doctors", doctor.Description);
        }

        // =====================================================
        //                   GetRoleNamesAsync
        // =====================================================

        [Fact]
        public async Task GetRoleNamesAsync_ReturnsNamesOrderedAlphabetically()
        {
            SetupRoles(
                CreateRole("r1", "Receptionist"),
                CreateRole("r2", "Admin"),
                CreateRole("r3", "Doctor"));

            var result = await _service.GetRoleNamesAsync(CancellationToken.None);

            Assert.Equal(new[] { "Admin", "Doctor", "Receptionist" }, result);
        }

        [Fact]
        public async Task GetRoleNamesAsync_ExcludesRolesWithNullName()
        {
            var nullNameRole = CreateRole("r2", "Temp");
            nullNameRole.Name = null;
            SetupRoles(CreateRole("r1", "Doctor"), nullNameRole);

            var result = await _service.GetRoleNamesAsync(CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("Doctor", result[0]);
        }

        [Fact]
        public async Task GetRoleNamesAsync_NoRoles_ReturnsEmptyList()
        {
            SetupRoles();

            var result = await _service.GetRoleNamesAsync(CancellationToken.None);

            Assert.Empty(result);
        }

        // =====================================================
        //                      EditAsync
        // =====================================================

        [Fact]
        public async Task EditAsync_RoleNotFound_ReturnsFailure()
        {
            _roleManagerMock.Setup(r => r.FindByIdAsync("missing")).ReturnsAsync((ApplicationRole?)null);
            var vm = new EditRoleViewModel { RoleId = "missing", RoleName = "X", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Role not found.", result.Errors);
            _roleManagerMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EditAsync_EmptyRoleName_ReturnsRequiredError(string? name)
        {
            var role = CreateRole();
            _roleManagerMock.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);
            var vm = new EditRoleViewModel { RoleId = role.Id, RoleName = name!, RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Role name is required.", result.Errors);
            _roleManagerMock.Verify(r => r.SetRoleNameAsync(It.IsAny<ApplicationRole>(), It.IsAny<string>()), Times.Never);
            _roleManagerMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_RenamingSuperAdmin_ReturnsFailure()
        {
            var role = CreateRole("r1", "SuperAdmin");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "Boss", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("The SuperAdmin role name cannot be changed.", result.Errors);
            _roleManagerMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_SuperAdminKeepsSameName_UpdatesDescriptionSuccessfully()
        {
            var role = CreateRole("r1", "SuperAdmin", "old");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.SetRoleNameAsync(role, "SuperAdmin")).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(r => r.UpdateAsync(role)).ReturnsAsync(IdentityResult.Success);
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "SuperAdmin", RoleDescription = "new" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.Equal("new", role.Description);
        }

        [Fact]
        public async Task EditAsync_NewNameAlreadyExists_ReturnsFailure()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Nurse")).ReturnsAsync(true);
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "Nurse", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("A role with this name already exists.", result.Errors);
            _roleManagerMock.Verify(r => r.SetRoleNameAsync(It.IsAny<ApplicationRole>(), It.IsAny<string>()), Times.Never);
            _roleManagerMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_SameNameDifferentCase_DoesNotCheckExistenceAndSucceeds()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.SetRoleNameAsync(role, "doctor")).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(r => r.UpdateAsync(role)).ReturnsAsync(IdentityResult.Success);
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "doctor", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.RoleExistsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_ValidEdit_UpdatesDescriptionNameAndSucceeds()
        {
            var role = CreateRole("r1", "Doctor", "old desc");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Surgeon")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.SetRoleNameAsync(role, "Surgeon")).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(r => r.UpdateAsync(role)).ReturnsAsync(IdentityResult.Success);
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "Surgeon", RoleDescription = "new desc" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.Equal("new desc", role.Description);
            _roleManagerMock.Verify(r => r.SetRoleNameAsync(role, "Surgeon"), Times.Once);
            _roleManagerMock.Verify(r => r.UpdateAsync(role), Times.Once);
        }

        [Fact]
        public async Task EditAsync_SetRoleNameFails_ReturnsErrorsAndDoesNotUpdate()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Surgeon")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.SetRoleNameAsync(role, "Surgeon"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid name" }));
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "Surgeon", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Invalid name", result.Errors);
            _roleManagerMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_UpdateFails_ReturnsErrors()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.RoleExistsAsync("Surgeon")).ReturnsAsync(false);
            _roleManagerMock.Setup(r => r.SetRoleNameAsync(role, "Surgeon")).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(r => r.UpdateAsync(role))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency conflict" }));
            var vm = new EditRoleViewModel { RoleId = "r1", RoleName = "Surgeon", RoleDescription = "d" };

            var result = await _service.EditAsync(vm, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains("Concurrency conflict", result.Errors);
        }

        // =====================================================
        //                    GetPagedAsync
        // =====================================================

        private static ApplicationRole[] CreateRoles(params string[] names) =>
            names.Select((n, i) => CreateRole($"r{i + 1}", n)).ToArray();

        [Fact]
        public async Task GetPagedAsync_ReturnsFirstPageOrderedByName()
        {
            SetupRoles(CreateRoles("E", "C", "A", "D", "B"));

            var result = await _service.GetPagedAsync(null, 1, 2, CancellationToken.None);

            Assert.Equal(new[] { "A", "B" }, result.Items.Select(i => i.Name));
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(2, result.PageSize);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_SkipsPreviousItems()
        {
            SetupRoles(CreateRoles("A", "B", "C", "D", "E"));

            var result = await _service.GetPagedAsync(null, 2, 2, CancellationToken.None);

            Assert.Equal(new[] { "C", "D" }, result.Items.Select(i => i.Name));
            Assert.Equal(2, result.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAsync_LastPartialPage_ReturnsRemainingItems()
        {
            SetupRoles(CreateRoles("A", "B", "C", "D", "E"));

            var result = await _service.GetPagedAsync(null, 3, 2, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal("E", result.Items.First().Name);
        }

        [Fact]
        public async Task GetPagedAsync_PageBeyondRange_ReturnsEmptyItemsButKeepsTotal()
        {
            SetupRoles(CreateRoles("A", "B"));

            var result = await _service.GetPagedAsync(null, 10, 5, CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(2, result.TotalCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task GetPagedAsync_PageLessThanOne_DefaultsToFirstPage(int page)
        {
            SetupRoles(CreateRoles("A", "B", "C"));

            var result = await _service.GetPagedAsync(null, page, 2, CancellationToken.None);

            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(new[] { "A", "B" }, result.Items.Select(i => i.Name));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPagedAsync_InvalidPageSize_DefaultsToSeven(int pageSize)
        {
            SetupRoles(CreateRoles("A", "B", "C", "D", "E", "F", "G", "H", "I"));

            var result = await _service.GetPagedAsync(null, 1, pageSize, CancellationToken.None);

            Assert.Equal(7, result.PageSize);
            Assert.Equal(7, result.Items.Count());
            Assert.Equal(9, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearch_FiltersByName()
        {
            SetupRoles(CreateRoles("Doctor", "Receptionist", "Doctor Assistant"));

            var result = await _service.GetPagedAsync("Doctor", 1, 10, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, i => Assert.Contains("Doctor", i.Name));
            Assert.Equal("Doctor", result.SearchTerm);
        }

        [Fact]
        public async Task GetPagedAsync_SearchWithNoMatch_ReturnsEmpty()
        {
            SetupRoles(CreateRoles("Doctor", "Receptionist"));

            var result = await _service.GetPagedAsync("zzz", 1, 10, CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPagedAsync_BlankSearch_ReturnsAllRoles(string? search)
        {
            SetupRoles(CreateRoles("Doctor", "Receptionist"));

            var result = await _service.GetPagedAsync(search, 1, 10, CancellationToken.None);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetPagedAsync_NoRoles_ReturnsEmptyResult()
        {
            SetupRoles();

            var result = await _service.GetPagedAsync(null, 1, 5, CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_MapsUserCountsOnlyForReturnedRoles()
        {
            SetupRoles(CreateRoles("A", "B", "C"));
            SeedUserRoles(("u1", "r1"), ("u2", "r1"), ("u3", "r2"), ("u4", "r3"));

            var result = await _service.GetPagedAsync(null, 1, 2, CancellationToken.None);

            Assert.Equal(2, result.Items.Single(i => i.Name == "A").UserCount);
            Assert.Equal(1, result.Items.Single(i => i.Name == "B").UserCount);
            Assert.DoesNotContain(result.Items, i => i.Name == "C");
        }

        [Fact]
        public async Task GetPagedAsync_MarksSuperAdminAsSystemRole()
        {
            SetupRoles(CreateRoles("SuperAdmin", "Doctor"));

            var result = await _service.GetPagedAsync(null, 1, 10, CancellationToken.None);

            Assert.True(result.Items.Single(i => i.Name == "SuperAdmin").IsSystemRole);
            Assert.False(result.Items.Single(i => i.Name == "Doctor").IsSystemRole);
        }

        // =====================================================
        //                    GetByIdAsync
        // =====================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByIdAsync_NullOrWhitespaceId_ReturnsNullWithoutQuerying(string? id)
        {
            var result = await _service.GetByIdAsync(id!, CancellationToken.None);

            Assert.Null(result);
            _roleManagerMock.Verify(r => r.Roles, Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_RoleNotFound_ReturnsNull()
        {
            SetupRoles(CreateRole("r1", "Doctor"));

            var result = await _service.GetByIdAsync("missing", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_RoleExists_ReturnsMappedViewModel()
        {
            SetupRoles(CreateRole("r1", "Doctor", "Doctors role"), CreateRole("r2", "Admin", "x"));

            var result = await _service.GetByIdAsync("r1", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("r1", result!.RoleId);
            Assert.Equal("Doctor", result.RoleName);
            Assert.Equal("Doctors role", result.RoleDescription);
        }

        // =====================================================
        //                  GetAllRolesAsync
        // =====================================================

        [Fact]
        public async Task GetAllRolesAsync_ReturnsIdNamePairsOrderedByName()
        {
            SetupRoles(
                CreateRole("r2", "Receptionist"),
                CreateRole("r1", "Doctor"),
                CreateRole("r3", "Admin"));

            var result = await _service.GetAllRolesAsync(CancellationToken.None);

            Assert.Equal(new[] { "Admin", "Doctor", "Receptionist" }, result.Select(r => r.Name));
            Assert.Equal("r3", result[0].Id);
            Assert.Equal("r1", result[1].Id);
            Assert.Equal("r2", result[2].Id);
        }

        [Fact]
        public async Task GetAllRolesAsync_NoRoles_ReturnsEmptyList()
        {
            SetupRoles();

            var result = await _service.GetAllRolesAsync(CancellationToken.None);

            Assert.Empty(result);
        }

        // =====================================================
        //                 GetRoleDetailsAsync
        // =====================================================

        [Fact]
        public async Task GetRoleDetailsAsync_RoleNotFound_ReturnsNull()
        {
            _roleManagerMock.Setup(r => r.FindByIdAsync("missing")).ReturnsAsync((ApplicationRole?)null);

            var result = await _service.GetRoleDetailsAsync("missing", CancellationToken.None);

            Assert.Null(result);
            _userManagerMock.Verify(u => u.GetUsersInRoleAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetRoleDetailsAsync_ReturnsOnlyPermissionClaims_TrimmedAndOrdered()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>
            {
                new Claim(PermissionConstants.ClaimType, "  Patients.View "),
                new Claim(PermissionConstants.ClaimType, "Appointments.Create"),
                new Claim("other", "ShouldBeIgnored")
            });
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Doctor"))
                .ReturnsAsync(new List<ApplicationUser>());

            var result = await _service.GetRoleDetailsAsync("r1", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(new[] { "Appointments.Create", "Patients.View" }, result!.Permissions);
            Assert.DoesNotContain("ShouldBeIgnored", result.Permissions);
        }

        [Fact]
        public async Task GetRoleDetailsAsync_ReturnsUsersOrderedByFullNameWithCount()
        {
            var role = CreateRole("r1", "Doctor");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Doctor")).ReturnsAsync(new List<ApplicationUser>
            {
                CreateUser("u2", "Zeinab", "z@test.com"),
                CreateUser("u1", "Ahmed", "a@test.com")
            });

            var result = await _service.GetRoleDetailsAsync("r1", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("r1", result!.Id);
            Assert.Equal("Doctor", result.Name);
            Assert.Equal(2, result.UsersCount);
            Assert.Equal(new[] { "Ahmed", "Zeinab" }, result.AssignedUsers.Select(u => u.FullName));
            Assert.Equal("u1", result.AssignedUsers[0].Id);
            Assert.Equal("a@test.com", result.AssignedUsers[0].Email);
        }

        [Fact]
        public async Task GetRoleDetailsAsync_NoClaimsAndNoUsers_ReturnsEmptyCollections()
        {
            var role = CreateRole("r1", "Receptionist");
            _roleManagerMock.Setup(r => r.FindByIdAsync("r1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Receptionist"))
                .ReturnsAsync(new List<ApplicationUser>());

            var result = await _service.GetRoleDetailsAsync("r1", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(0, result!.UsersCount);
            Assert.Empty(result.Permissions);
            Assert.Empty(result.AssignedUsers);
        }
    }
}
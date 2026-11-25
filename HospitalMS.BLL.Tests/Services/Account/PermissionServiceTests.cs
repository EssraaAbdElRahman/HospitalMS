using HospitalMS.BLL.Helper;
using Microsoft.AspNetCore.Identity;
using MockQueryable;
using System.Security.Claims;

namespace HospitalMS.BLL.Tests.Services.Permission
{
    public class PermissionServiceTests
    {
        private readonly Mock<IRoleStore<ApplicationRole>> _roleStoreMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly PermissionService _service;

        public PermissionServiceTests()
        {
            _roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();

            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
                _roleStoreMock.Object,
                null, null, null, null);

            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _service = new PermissionService(
                _roleManagerMock.Object,
                _unitOfWorkMock.Object);
        }

        private static ApplicationRole CreateRole(string id = "role1", string name = "Doctor")
        {            
            var role = (ApplicationRole)Activator.CreateInstance(typeof(ApplicationRole), nonPublic: true)!;
            role.Id = id;
            role.Name = name;
            role.NormalizedName = name.ToUpper();
            return role;
        }

        private static string[] ValidKeys() =>
            PermissionService.BuildModules()
                .SelectMany(m => m.Columns.Select(c => c.Key))
                .ToArray();

        private void SetupRoles(params ApplicationRole[] roles) =>
            _roleManagerMock.Setup(r => r.Roles)
                .Returns(roles.ToList().BuildMock());

        private void SetupFindById(ApplicationRole role) =>
            _roleManagerMock.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);

        // ---------------- BuildModules ----------------

        [Fact]
        public void BuildModules_ReturnsModulesWithColumns()
        {
            // Act
            var result = PermissionService.BuildModules();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, module => Assert.NotNull(module.Columns));
        }

        // ---------------- GetMatrixAsync ----------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetMatrixAsync_NullOrEmptyRoleId_ReturnsModulesWithoutQueryingRoles(string? roleId)
        {
            // Act
            var result = await _service.GetMatrixAsync(roleId, CancellationToken.None);

            // Assert
            Assert.NotEmpty(result.Modules);
            Assert.Empty(result.RoleNames);
            Assert.Empty(result.RolePermissions);
            _roleManagerMock.Verify(r => r.Roles, Times.Never);
        }

        [Fact]
        public async Task GetMatrixAsync_RoleExists_ReturnsOnlyPermissionClaims()
        {
            // Arrange
            var role = CreateRole("role1", "Doctor");
            SetupRoles(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("permission", "Patients.View"),
                    new Claim("permission", "Patients.Create"),
                    new Claim("other", "SomeValue")
                });

            // Act
            var result = await _service.GetMatrixAsync(role.Id, CancellationToken.None);

            // Assert
            Assert.Single(result.RoleNames);
            Assert.Equal("Doctor", result.RoleNames[0]);
            Assert.Contains("Patients.View", result.RolePermissions["Doctor"]);
            Assert.Contains("Patients.Create", result.RolePermissions["Doctor"]);
            Assert.DoesNotContain("SomeValue", result.RolePermissions["Doctor"]);
        }

        [Fact]
        public async Task GetMatrixAsync_RoleNotFound_ReturnsEmptyRoleData()
        {
            // Arrange
            SetupRoles(); 

            // Act
            var result = await _service.GetMatrixAsync("not-found", CancellationToken.None);

            // Assert
            Assert.NotEmpty(result.Modules);
            Assert.Empty(result.RoleNames);
            Assert.Empty(result.RolePermissions);
        }

        [Fact]
        public async Task GetMatrixAsync_RoleWithoutPermissionClaims_ReturnsEmptyPermissions()
        {
            // Arrange
            var role = CreateRole("role1", "Receptionist");
            SetupRoles(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            // Act
            var result = await _service.GetMatrixAsync(role.Id, CancellationToken.None);

            // Assert
            Assert.Single(result.RoleNames);
            Assert.True(result.RolePermissions.ContainsKey("Receptionist"));
            Assert.Empty(result.RolePermissions["Receptionist"]);
        }

        [Fact]
        public async Task GetMatrixAsync_MultipleRoles_ReturnsOnlyRequestedRole()
        {
            // Arrange
            var doctor = CreateRole("role1", "Doctor");
            var receptionist = CreateRole("role2", "Receptionist");
            SetupRoles(doctor, receptionist);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(doctor))
                .ReturnsAsync(new List<Claim> { new Claim("permission", "Patients.View") });

            // Act
            var result = await _service.GetMatrixAsync("role1", CancellationToken.None);

            // Assert
            Assert.Single(result.RoleNames);
            Assert.Equal("Doctor", result.RoleNames[0]);
            _roleManagerMock.Verify(r => r.GetClaimsAsync(receptionist), Times.Never);
        }

        // ---------------- UpdateMatrixAsync ----------------

        [Fact]
        public async Task UpdateMatrixAsync_RoleNotFound_ReturnsFailure()
        {
            // Arrange
            _roleManagerMock.Setup(r => r.FindByIdAsync("missing")).ReturnsAsync((ApplicationRole?)null);

            // Act
            var result = await _service.UpdateMatrixAsync("missing", new[] { ValidKeys()[0] }, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Role not found.", result.Errors);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_SuperAdmin_ReturnsFailureAndDoesNotChangePermissions()
        {
            // Arrange
            var role = CreateRole("role3", "SuperAdmin");
            SetupFindById(role);

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { ValidKeys()[0] }, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("SuperAdmin permissions cannot be modified.", result.Errors);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _roleManagerMock.Verify(r => r.GetClaimsAsync(It.IsAny<ApplicationRole>()), Times.Never);
            _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
            _roleManagerMock.Verify(r => r.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_AddsSelectedPermission_WhenClaimDoesNotExist()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());
            _roleManagerMock.Setup(r => r.AddClaimAsync(role, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { key }, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.AddClaimAsync(
                role,
                It.Is<Claim>(c => c.Type == PermissionConstants.ClaimType && c.Value == key)),
                Times.Once);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_RemovesExistingPermission_WhenNotSelected()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            var existingClaim = new Claim(PermissionConstants.ClaimType, key);
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim> { existingClaim });
            _roleManagerMock.Setup(r => r.RemoveClaimAsync(role, existingClaim))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, Array.Empty<string>(), CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.RemoveClaimAsync(role, existingClaim), Times.Once);
            _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMatrixAsync_DoesNotAddOrRemove_WhenSelectionMatchesExisting()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role))
                .ReturnsAsync(new List<Claim> { new Claim(PermissionConstants.ClaimType, key) });

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { key }, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
            _roleManagerMock.Verify(r => r.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMatrixAsync_InvalidKeys_AreIgnored()
        {
            // Arrange
            var role = CreateRole();
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { "Not.A.Real.Permission" }, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_NullSelection_TreatedAsEmptyAndRemovesExisting()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            var existingClaim = new Claim(PermissionConstants.ClaimType, key);
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim> { existingClaim });
            _roleManagerMock.Setup(r => r.RemoveClaimAsync(role, existingClaim))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, null!, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _roleManagerMock.Verify(r => r.RemoveClaimAsync(role, existingClaim), Times.Once);
        }

        [Fact]
        public async Task UpdateMatrixAsync_AddClaimFails_RollsBackAndReturnsErrors()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());
            _roleManagerMock.Setup(r => r.AddClaimAsync(role, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Add failed" }));

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { key }, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Add failed", result.Errors);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_RemoveClaimFails_RollsBackAndReturnsErrors()
        {
            // Arrange
            var key = ValidKeys()[0];
            var role = CreateRole();
            var existingClaim = new Claim(PermissionConstants.ClaimType, key);
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role)).ReturnsAsync(new List<Claim> { existingClaim });
            _roleManagerMock.Setup(r => r.RemoveClaimAsync(role, existingClaim))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Remove failed" }));

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, Array.Empty<string>(), CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Remove failed", result.Errors);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMatrixAsync_UnexpectedException_RollsBackAndReturnsGenericFailure()
        {
            // Arrange
            var role = CreateRole();
            SetupFindById(role);
            _roleManagerMock.Setup(r => r.GetClaimsAsync(role))
                .ThrowsAsync(new InvalidOperationException("DB error"));

            // Act
            var result = await _service.UpdateMatrixAsync(role.Id, new[] { ValidKeys()[0] }, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("An error occurred while updating permissions.", result.Errors);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
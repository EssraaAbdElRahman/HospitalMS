using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Tests.Services.Account
{
    public class UserAccountServiceTests : AccountServiceTestsBase
    {
        private readonly UserAccountService _service;

        public UserAccountServiceTests()
        {
            _service = new UserAccountService(
                UserManagerMock.Object,
                RoleManagerMock.Object,
                DoctorServiceMock.Object,
                ReceptionistServiceMock.Object,
                Context,
                UnitOfWorkMock.Object);
        }


        private static ApplicationUser CreateUser(
            string id = "user1",
            string fullName = "Test User",
            string email = "test@test.com",
            UserType userType = UserType.Staff,
            StaffType? staffType = StaffType.Doctor)
        {
            var user = ApplicationUser.CreateStaff(
                fullName,
                email,
                userType,
                staffType,
                "admin1");

            // ApplicationUser.Id  private setter
            typeof(ApplicationUser)
                .GetProperty(nameof(ApplicationUser.Id))!
                .SetValue(user, id);

            return user;
        }

        private static ApplicationRole CreateRole(string id = "role1", string name = "Doctor")
        {
            var role = (ApplicationRole)Activator.CreateInstance(typeof(ApplicationRole), nonPublic: true)!;
            typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.Id))!.SetValue(role, id);
            typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.Name))!.SetValue(role, name);
            typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.NormalizedName))!.SetValue(role, name.ToUpper());
            return role;
        }
        // ---------------- GetUserDetailsAsync ----------------

        [Fact]
        public async Task GetUserDetailsAsync_UserNotFound_ReturnsNull()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _service.GetUserDetailsAsync("user1", CancellationToken.None);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserDetailsAsync_ExistingUser_ReturnsMappedViewModel()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Doctor" });

            //Act
            var result = await _service.GetUserDetailsAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.UserType, result.UserType);
            Assert.Equal(user.StaffType, result.StaffType);
            Assert.Equal("Doctor", result.RoleName);
            Assert.Equal(user.Status, result.Status);
            Assert.Equal(
                user.PasswordResetStatus,
                result.PasswordResetStatus);
        }

        [Fact]
        public async Task GetUserDetailsAsync_UserHasNoRoles_ReturnsNARole()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            //Act
            var result = await _service.GetUserDetailsAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.NotNull(result);
            Assert.Equal("N/A", result!.RoleName);
        }

        // ---------------- GetByIdAsync ----------------

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ReturnsNull()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _service.GetByIdAsync("user1", CancellationToken.None);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsMappedViewModel()
        {
            //Arrange
            var user = CreateUser();

            var role = CreateRole();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { role.Name! });

            RoleManagerMock
                .Setup(r => r.FindByNameAsync(role.Name!))
                .ReturnsAsync(role);

            //Act
            var result = await _service.GetByIdAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(role.Id, result.Role);
        }

        [Fact]
        public async Task GetByIdAsync_UserHasNoRole_ReturnsEmptyRole()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            //Act
            var result = await _service.GetByIdAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result!.Role);
        }

        // ---------------- GetForStatusUpdateAsync ----------------

        [Fact]
        public async Task GetForStatusUpdateAsync_UserNotFound_ReturnsFailure()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _service.GetForStatusUpdateAsync("user1", CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task GetForStatusUpdateAsync_PendingUser_ReturnsFailure()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            //Act
            var result = await _service.GetForStatusUpdateAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                "Pending accounts must be activated",
                result.Errors.First());
        }

        [Fact]
        public async Task GetForStatusUpdateAsync_ActiveUser_ReturnsSuccess()
        {
            //Arrange
            var user = CreateUser();

            user.SetAccountStatus(
                AccountStatus.Active,
                "admin1");

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            //Act
            var result = await _service.GetForStatusUpdateAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Id, result.Data!.Id);
            Assert.Equal(user.FullName, result.Data.FullName);
            Assert.Equal(
                user.Status,
                result.Data.CurrentStatus);
        }

        // ---------------- UpdateStatusAsync ----------------

        [Fact]
        public async Task UpdateStatusAsync_UserNotFound_ReturnsFailure()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _service.UpdateStatusAsync(
                "user1",
                AccountStatus.Active,
                "admin1",
                CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);

            UserManagerMock.Verify(
                u => u.UpdateAsync(It.IsAny<ApplicationUser>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidTransition_ReturnsFailure()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            //Act
            var result = await _service.UpdateStatusAsync(
                user.Id,
                AccountStatus.Pending,
                "admin1",
                CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);

            UserManagerMock.Verify(
                u => u.UpdateAsync(It.IsAny<ApplicationUser>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ValidTransition_UpdatesUserAndReturnsSuccess()
        {
            //Arrange
            var user = CreateUser();

            user.SetAccountStatus(
                AccountStatus.Active,
                "admin1");

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            //Act
            var result = await _service.UpdateStatusAsync(
                user.Id,
                AccountStatus.OnHold,
                "admin1",
                CancellationToken.None);

            //Assert
            Assert.True(result.Succeeded);
            Assert.Equal(AccountStatus.OnHold, user.Status);

            UserManagerMock.Verify(
                u => u.UpdateAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdateFails_ReturnsFailure()
        {
            //Arrange
            var user = CreateUser();

            user.SetAccountStatus(
                AccountStatus.Active,
                "admin1");

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.UpdateAsync(user))
                .ReturnsAsync(
                    IdentityResult.Failed(
                        new IdentityError
                        {
                            Description = "Update failed."
                        }));

            //Act
            var result = await _service.UpdateStatusAsync(
                                                        user.Id,
                                                        AccountStatus.OnHold,
                                                        "admin1",
                                                        CancellationToken.None);
            //Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                "Update failed.",
                result.Errors.First());
        }

        // ---------------- GenerateActivationLinkAsync ----------------

        [Fact]
        public async Task GenerateActivationLinkAsync_UserNotFound_ReturnsFailure()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result =
                await _service.GenerateActivationLinkAsync("user1", CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task GenerateActivationLinkAsync_NonPendingUser_ReturnsFailure()
        {
            //Arrange
            var user = CreateUser();

            user.SetAccountStatus(
                AccountStatus.Active,
                "admin1");

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            //Act
            var result =
                await _service.GenerateActivationLinkAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                "Activation link can only be generated",
                result.Errors.First());

            UserManagerMock.Verify(
                u => u.GeneratePasswordResetTokenAsync(
                    It.IsAny<ApplicationUser>()),
                Times.Never);
        }

        [Fact]
        public async Task GenerateActivationLinkAsync_PendingUser_ReturnsToken()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("activation-token");

            //Act
            var result =
                await _service.GenerateActivationLinkAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Id, result.Data!.UserId);
            Assert.Equal("activation-token", result.Data.Token);

            UserManagerMock.Verify(
                u => u.GeneratePasswordResetTokenAsync(user),
                Times.Once);
        }

        // ---------------- GeneratePasswordResetLinkAsync ----------------

        [Fact]
        public async Task GeneratePasswordResetLinkAsync_UserNotFound_ReturnsFailure()
        {
            //Arrange
            UserManagerMock
                .Setup(u => u.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result =
                await _service.GeneratePasswordResetLinkAsync("user1", CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task GeneratePasswordResetLinkAsync_NoPendingRequest_ReturnsFailure()
        {
            //Arrange
            var user = CreateUser();

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            //Act
            var result =
                await _service.GeneratePasswordResetLinkAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                "There is no pending password reset request",
                result.Errors.First());
        }

        [Fact]
        public async Task GeneratePasswordResetLinkAsync_PendingRequest_ReturnsToken()
        {
            //Arrange
            var user = CreateUser();
            typeof(ApplicationUser).GetProperty("PasswordResetStatus")!
               .SetValue(user, PasswordResetRequestStatus.Pending);

            UserManagerMock
                .Setup(u => u.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            UserManagerMock
                .Setup(u => u.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token");

            //Act
            var result =
                await _service.GeneratePasswordResetLinkAsync(user.Id, CancellationToken.None);

            //Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Id, result.Data!.UserId);
            Assert.Equal("reset-token", result.Data.Token);

            UserManagerMock.Verify(
                u => u.GeneratePasswordResetTokenAsync(user),
                Times.Once);
        }
    }
}
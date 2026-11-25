using HospitalMS.BLL.ModelVM.Login;
using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Tests.Services.Account
{
    public class LoginAsyncTests : AccountServiceTestsBase
    {
        private const string Email = "user@test.com";

        private static ApplicationUser CreateUser(
            AccountStatus status = AccountStatus.Active,
            UserType userType = UserType.Staff,
            StaffType? staffType = StaffType.Doctor)
        {
            var user = ApplicationUser.CreateStaff("Test User", Email, userType, staffType, "admin1");
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Status))!.SetValue(user, status);
            return user;
        }

        private void SetupUserAndPassword(ApplicationUser user, SignInResult passwordResult)
        {
            UserManagerMock.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(user);
            SignInManagerMock.Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
                .ReturnsAsync(passwordResult);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            UserManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            var service = CreateService();
            var vm = new LoginViewModel { Email = "notfound@test.com", Password = "123456" };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Invalid credentials.", result.Errors);
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsFailure()
        {
            // Arrange
            var user = CreateUser();
            SetupUserAndPassword(user, SignInResult.Failed);
            var service = CreateService();
            var vm = new LoginViewModel { Email = Email, Password = "wrongpass" };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Invalid credentials.", result.Errors);
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_AccountLockedOut_ReturnsLockedOutMessage()
        {
            // Arrange
            var user = CreateUser();
            SetupUserAndPassword(user, SignInResult.LockedOut);
            var service = CreateService();
            var vm = new LoginViewModel { Email = Email, Password = "123456" };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains("locked", StringComparison.OrdinalIgnoreCase));
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Theory]
        [InlineData(AccountStatus.Pending, "not activated")]
        [InlineData(AccountStatus.OnHold, "on hold")]
        [InlineData(AccountStatus.Suspended, "suspended")]
        public async Task LoginAsync_AccountNotActive_ReturnsCorrectMessage(AccountStatus status, string expectedKeyword)
        {
            // Arrange
            var user = CreateUser(status);
            SetupUserAndPassword(user, SignInResult.Success);
            var service = CreateService();
            var vm = new LoginViewModel { Email = Email, Password = "123456" };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase));
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessAndSignsIn()
        {
            // Arrange
            var user = CreateUser(AccountStatus.Active, UserType.Staff, StaffType.Doctor);
            SetupUserAndPassword(user, SignInResult.Success);
            SignInManagerMock.Setup(s => s.SignInAsync(user, false, null)).Returns(Task.CompletedTask);
            var service = CreateService();
            var vm = new LoginViewModel
            {
                Email = Email,
                Password = "correctpass",
                RememberMe = false,
                ReturnUrl = "/dashboard"
            };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(UserType.Staff, result.Data!.UserType);
            Assert.Equal(StaffType.Doctor, result.Data.StaffType);
            Assert.Equal("/dashboard", result.Data.ReturnUrl);
            Assert.False(result.Data.IsLockedOut);
            SignInManagerMock.Verify(s => s.SignInAsync(user, false, null), Times.Once);
            // مفيش reset request معلّق، فمفروض مفيش update
            UserManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoginAsync_RememberMe_IsPassedAsIsPersistent(bool rememberMe)
        {
            // Arrange
            var user = CreateUser();
            SetupUserAndPassword(user, SignInResult.Success);
            SignInManagerMock.Setup(s => s.SignInAsync(user, rememberMe, null)).Returns(Task.CompletedTask);
            var service = CreateService();
            var vm = new LoginViewModel { Email = Email, Password = "pass", RememberMe = rememberMe };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            SignInManagerMock.Verify(s => s.SignInAsync(user, rememberMe, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_PendingResetRequest_IsClearedOnSuccessfulLogin()
        {
            // Arrange
            var user = CreateUser();
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.PasswordResetStatus))!
                .SetValue(user, PasswordResetRequestStatus.Pending);
            SetupUserAndPassword(user, SignInResult.Success);
            SignInManagerMock.Setup(s => s.SignInAsync(user, false, null)).Returns(Task.CompletedTask);
            UserManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            var service = CreateService();
            var vm = new LoginViewModel { Email = Email, Password = "pass" };

            // Act
            var result = await service.LoginAsync(vm, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(PasswordResetRequestStatus.None, user.PasswordResetStatus);
            UserManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
        }
    }
}
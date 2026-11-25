using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Tests.Services.Account
{
    public class RequestPasswordResetAsyncTests : AccountServiceTestsBase
    {
        private static ApplicationUser CreateUser(AccountStatus status, UserType userType,
                                              StaffType? staffType = null, string email = "user@test.com")
        {
            var user = ApplicationUser.CreateStaff("Test User", email, userType, staffType, "admin1");
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Status))!.SetValue(user, status);
            return user;
        }

        [Fact]
        public async Task RequestPasswordReset_UserNotFound_ReturnsSuccessAnyway()
        {
            // Arrange
            UserManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            var service = CreateService();

            // Act
            var result = await service.RequestPasswordResetAsync("noone@test.com", CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            UserManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task RequestPasswordReset_ActivePatient_ReturnsSuccessWithoutUpdating()
        {
            // Arrange 
            var patient = CreateUser(AccountStatus.Active, UserType.Patient);
            UserManagerMock.Setup(m => m.FindByEmailAsync(patient.Email!)).ReturnsAsync(patient);
            var service = CreateService();

            // Act
            var result = await service.RequestPasswordResetAsync(patient.Email!, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(PasswordResetRequestStatus.Pending, patient.PasswordResetStatus);
            UserManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Theory]
        [InlineData(AccountStatus.Pending)]
        [InlineData(AccountStatus.OnHold)]
        [InlineData(AccountStatus.Suspended)]
        public async Task RequestPasswordReset_NonActiveStaff_ReturnsSuccessWithoutUpdating(AccountStatus status)
        {
            // Arrange
            var user = CreateUser(status, UserType.Staff, StaffType.Doctor);
            UserManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            var service = CreateService();

            // Act
            var result = await service.RequestPasswordResetAsync(user.Email!, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            UserManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Theory]
        [InlineData(StaffType.Doctor)]
        [InlineData(StaffType.Receptionist)]
        public async Task RequestPasswordReset_ActiveStaff_SetsStatusToPendingAndUpdates(StaffType staffType)
        {
            // Arrange
            var user = CreateUser(AccountStatus.Active, UserType.Staff, staffType, "staff@test.com");
            UserManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            var service = CreateService();

            // Act
            var result = await service.RequestPasswordResetAsync(user.Email!, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(PasswordResetRequestStatus.Pending, user.PasswordResetStatus);
            UserManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task RequestPasswordReset_UpdateFails_ReturnsFailureWithErrors()
        {
            // Arrange
            var user = CreateUser(AccountStatus.Active, UserType.Staff, StaffType.Doctor);
            UserManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency conflict" }));
            var service = CreateService();

            // Act
            var result = await service.RequestPasswordResetAsync(user.Email!, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Concurrency conflict", result.Errors);
        }
    }
}
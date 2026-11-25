using HospitalMS.BLL.ModelVM.Account;
using Microsoft.AspNetCore.Identity;

namespace HospitalMS.BLL.Tests.Services.Account
{
    public class ActivateAccountAsyncTests : AccountServiceTestsBase
    {
        private static ApplicationUser CreatePendingUser(string id = "1")
        {
            var user = ApplicationUser.CreateStaff("Test User", "test@test.com", UserType.Staff, StaffType.Doctor, "admin1");
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Id))!.SetValue(user, id);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Status))!.SetValue(user, AccountStatus.Pending);
            return user;
        }

        private static ApplicationUser CreateActiveUser(string id = "1")
        {
            var user = CreatePendingUser(id);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Status))!.SetValue(user, AccountStatus.Active);
            return user;
        }

        [Fact]
        public async Task ActivateAccount_UserNotFound_ReturnsFailure()
        {
            // Arrange
            UserManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "not-exist", Token = "token", Password = "NewPass123!" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("This activation link is invalid or has expired.", result.Errors);
            UserManagerMock.Verify(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActivateAccount_UserNotPending_ReturnsFailure()
        {
            // Arrange
            var user = CreateActiveUser();
            UserManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "1", Token = "token", Password = "NewPass123!" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("This activation link is invalid or has expired.", result.Errors);
            UserManagerMock.Verify(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActivateAccount_ValidPendingUser_ActivatesCommitsAndSignsIn()
        {
            // Arrange
            var user = CreatePendingUser();
            UserManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.ResetPasswordAsync(user, "token", "NewPass123!"))
                .ReturnsAsync(IdentityResult.Success);
            UserManagerMock.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            SignInManagerMock.Setup(s => s.SignInAsync(user, false, null))
                .Returns(Task.CompletedTask);
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "1", Token = "token", Password = "NewPass123!" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(AccountStatus.Active, user.Status);
            UserManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
            UnitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            SignInManagerMock.Verify(s => s.SignInAsync(user, false, null), Times.Once);
        }

        [Fact]
        public async Task ActivateAccount_ResetPasswordFails_RollsBackAndDoesNotSignIn()
        {
            // Arrange
            var user = CreatePendingUser();
            UserManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.ResetPasswordAsync(user, "token", "weak"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "1", Token = "token", Password = "weak" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Password too weak", result.Errors);
            Assert.Equal(AccountStatus.Pending, user.Status);
            UserManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Fact]
        public async Task ActivateAccount_UpdateAsyncFails_RollsBackAndDoesNotSignIn()
        {
            // Arrange
            var user = CreatePendingUser();
            UserManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.ResetPasswordAsync(user, "token", "NewPass123!"))
                .ReturnsAsync(IdentityResult.Success);
            UserManagerMock.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency conflict" }));
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "1", Token = "token", Password = "NewPass123!" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Concurrency conflict", result.Errors);
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        [Fact]
        public async Task ActivateAccount_UnexpectedException_RollsBackAndReturnsGenericFailure()
        {
            // Arrange
            var user = CreatePendingUser();
            UserManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            UserManagerMock.Setup(m => m.ResetPasswordAsync(user, "token", "NewPass123!"))
                .ThrowsAsync(new InvalidOperationException("Unexpected DB error"));
            var service = CreateService();
            var vm = new SetPasswordViewModel { UserId = "1", Token = "token", Password = "NewPass123!" };

            // Act
            var result = await service.ActivateAccountAsync(vm, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("An error occurred while activating the account.", result.Errors);
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            SignInManagerMock.Verify(s => s.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }
    }
}
using HospitalMS.DAL.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalMS.BLL.Tests.Services.Account
{
    public abstract class AccountServiceTestsBase : IDisposable
    {
        protected readonly Mock<IUserStore<ApplicationUser>> UserStoreMock;
        protected readonly Mock<UserManager<ApplicationUser>> UserManagerMock;

        protected readonly Mock<IRoleStore<ApplicationRole>> RoleStoreMock;
        protected readonly Mock<RoleManager<ApplicationRole>> RoleManagerMock;

        protected readonly Mock<IDoctorService> DoctorServiceMock;
        protected readonly Mock<IReceptionistService> ReceptionistServiceMock;
        protected readonly Mock<SignInManager<ApplicationUser>> SignInManagerMock;
        protected readonly HospitalDbContext Context;
        protected readonly Mock<IHttpContextAccessor> HttpContextAccessorMock;
        protected readonly Mock<IUserClaimsPrincipalFactory<ApplicationUser>> ClaimsFactoryMock;
        protected readonly Mock<IOptions<IdentityOptions>> OptionsMock;
        protected readonly Mock<ILogger<SignInManager<ApplicationUser>>> LoggerMock;
        protected readonly Mock<IAuthenticationSchemeProvider> AuthenticationSchemeProviderMock;
        protected readonly Mock<IUserConfirmation<ApplicationUser>> UserConfirmationMock;
        protected Mock<IUnitOfWork> UnitOfWorkMock { get; } = new();

        protected AccountService CreateService() =>
            new AccountService(UserManagerMock.Object, SignInManagerMock.Object, UnitOfWorkMock.Object);
        protected AccountServiceTestsBase()
        {
            UserStoreMock = new Mock<IUserStore<ApplicationUser>>();

            UserManagerMock = new Mock<UserManager<ApplicationUser>>(
                UserStoreMock.Object,
                null, null, null, null, null, null, null, null);

            RoleStoreMock = new Mock<IRoleStore<ApplicationRole>>();

            RoleManagerMock = new Mock<RoleManager<ApplicationRole>>(
                RoleStoreMock.Object,
                null, null, null, null);
            HttpContextAccessorMock = new Mock<IHttpContextAccessor>();
            ClaimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            OptionsMock = new Mock<IOptions<IdentityOptions>>();
            OptionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
            LoggerMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            AuthenticationSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
            UserConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();
            SignInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                            UserManagerMock.Object,
                            HttpContextAccessorMock.Object,
                            ClaimsFactoryMock.Object,
                            OptionsMock.Object,
                            LoggerMock.Object,
                            AuthenticationSchemeProviderMock.Object);
            DoctorServiceMock = new Mock<IDoctorService>();
            ReceptionistServiceMock = new Mock<IReceptionistService>();

            Context = CreateInMemoryContext();
        }

        protected static HospitalDbContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<HospitalDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new HospitalDbContext(options);

            context.Database.EnsureCreated();

            return context;
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
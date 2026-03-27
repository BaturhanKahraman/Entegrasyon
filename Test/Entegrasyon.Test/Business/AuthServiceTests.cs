using Entegrasyon.Entity.User;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Concrete.Auth;
using Moq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Business.Utility.Constants;

namespace Entegrasyon.UnitTest.Business
{
    public class AuthServiceTests : BaseTest
    {
        private readonly IAuthService authService;
        public AuthServiceTests()
        {
            authService = new AuthService(mockContextFactory.Object, mockApplicationLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCrediantials_ReturnsSuccessDataResultOfUserLoginSuccessDto()
        {
            //Arrange
            string userName = "test", password="testpassword";
            ApplicationUser user = CreateUser(userName,password);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(userName,password);
            //Assert
            result.Should().BeOfType<SuccessDataResult<UserLoginSuccessDto>>();
            result.As<SuccessDataResult<UserLoginSuccessDto>>().Data.Id.Should().Be(user.Id);
            result.Success.Should().Be(true);
        }

   

        [Fact]
        public async Task LoginAsync_WithInValidUserName_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string wrongUserName = "test1";
            string password = "testPassword";
            ApplicationUser user = CreateUser(userName,password);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(wrongUserName, password);
            //Assert
            result.Should().BeOfType<ErrorResult>();
        }
        [Fact]
        public async Task LoginAsync_WithInValidPassword_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string password = "testPassword";
            string wrongPassword = "testpassword1";
            ApplicationUser user = CreateUser(userName,password);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(userName,wrongPassword);
            //Assert
            result.Should().BeOfType<ErrorResult>();
        }

        [Fact]
        public async Task LogicAsync_ValidCredentialUserNeedsToTakeNewPassword_ReturnsSuccessDataResultOfLoginNewPasswordDto()
        {
            //arrange
            string userName = "test";
            string password = "testpassword";
            string tempPassword = "tempPassword";
            ApplicationUser user = CreateUser(userName,password,tempPassword,true);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //act
            var result = await authService.LoginAsync(userName, tempPassword);
            //arrange
            result.Should().BeOfType<SuccessDataResult<LoginNewPasswordDto>>();
            result.As<SuccessDataResult<LoginNewPasswordDto>>().Data.UserId.Should().Be(user.Id.ToString());
        }

        [Fact]
        public async Task LogicAsync_InValidCredentialUserNeedsToTakeNewPassword_ReturnsErrorResult()
        {
            //arrange
            string userName = "test";
            string password = "testpassword";
            string tempPassword = "tempPassword";
            string wrongTempPassword = "wrongTempPassword";
            ApplicationUser user = CreateUser(userName,password,tempPassword,true);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //act
            var result = await authService.LoginAsync(userName, wrongTempPassword);
            //arrange
            result.Should().BeOfType<ErrorResult>();
        }

        [Fact]
        public async Task LoginAsync_InactiveUser_ReturnsErrorResult()
        {
            //arrange
            string userName = "test";
            string password = "testpassword";
            ApplicationUser user = CreateUser(userName,password,isActive: false);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);
            //act
            var result = await authService.LoginAsync(userName, password);
            //arrange
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.UserIsInactive);
        }

        [Fact]
        public async Task AssignTempPassword_WithValidGuid_ReturnsSuccessResult()
        {
            // Arrange
            string userName = "userName", password="password", newPassword = "newpassword";
            ApplicationUser user = CreateUser(userName,password);
            mockIntegrationDbContext.Setup(c => c.Users.FindAsync(user.Id)).ReturnsAsync(user);

            // Act
            var result = await authService.AssignTempPassword(newPassword,user.Id.ToString());

            // Assert
            result.Should().BeOfType<SuccessResult>();
            result.Success.Should().BeTrue();
            mockIntegrationDbContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
            user.NeedsTakeNewPassword.Should().BeTrue();
            user.TemporaryPassword.Should().Be(newPassword);
        }
        [Fact]
        public async Task AssignTempPassword_WithInvalidUserId_ReturnsErrorResult()
        {
            // Arrange
            string password = "newPassword";
            string invalidUserId = "invalidUserId";

            // Act
            var result = await authService.AssignTempPassword(password, invalidUserId);

            // Assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            mockIntegrationDbContext.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task AssignTempPassword_WithNonExistingUser_ReturnsErrorResult()
        {
            // Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            mockIntegrationDbContext.Setup(c => c.Users.FindAsync(userId)).Returns(null!);

            // Act
            var result = await authService.AssignTempPassword(password, userId.ToString());

            // Assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            mockIntegrationDbContext.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CreatePassword_WithValidPassword_ReturnsSuccessResult()
        {
            //Arrange
            string userName="test",password="password";
            ApplicationUser user = CreateUser(userName,password);
            mockIntegrationDbContext.Setup(db => db.Users.FindAsync(new object[] { user.Id}, default)).ReturnsAsync(user);
            //act
            var result = await authService.CreatePassword(password, user.Id, default);
            //assert
            result.Should().BeOfType<SuccessResult>();
            result.Message.Should().Be(Messages.FirstPasswordAssigned);
            user.PasswordHash.Should().NotBeNull();
            user.PasswordSalt.Should().NotBeNull();
            user.NeedsTakeNewPassword.Should().BeFalse();
            mockIntegrationDbContext.Verify(ctx => ctx.SaveChangesAsync(default), Times.Once);
        }
        [Fact]
        public async Task CreatePassword_WithNotExistingUser_ReturnErrorResult()
        {
            //Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            mockIntegrationDbContext.Setup(db => db.Users.FindAsync(new object[] { userId }, default)).Returns(null!);
            //act
            var result = await authService.CreatePassword(password, userId, default);
            //assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.UserNotFound);
            mockIntegrationDbContext.Verify(ctx => ctx.SaveChangesAsync(default), Times.Never);
        }


        [Theory]
        [InlineData("")]
        [InlineData(null!)]
        public async Task CreatePassword_WithInvalidPassword_ReturnsErrorResult(string? password)
        {
            //act
            var result = await authService.CreatePassword(password!, default, default);
            //assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            mockIntegrationDbContext.Verify(ctx => ctx.SaveChangesAsync(default), Times.Never);
        }

        private ApplicationUser CreateUser(string userName,string password,string tempPassword="",bool needsToTakePassword=false,bool isActive = true)
        {
            HashingHelper.CreatePasswordHash(password,out var passwordHash,out var passwordSalt);
            return new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = needsToTakePassword,
                TemporaryPassword = tempPassword,
                IsActive = isActive,
                Roles = new List<Role>()
            };
        }
    }
}
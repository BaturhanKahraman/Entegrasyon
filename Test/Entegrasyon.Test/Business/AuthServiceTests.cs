using Entegrasyon.Entity.User;
using Shared.Security;
using Moq.EntityFrameworkCore;
using Entegrasyon.Business.Concrete.Auth;
using Moq;
using FluentAssertions;
using Shared.Results;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Auth;
using Shared.User.Dto;
using Entegrasyon.Business.Utility.Constants;

namespace Entegrasyon.UnitTest.Business
{
    public class AuthServiceTests : BaseTest
    {
        private readonly IAuthService authService;
        public AuthServiceTests()
        {
            authService = new AuthService(integrationDbContextMock.Object, applicationLoggerMock.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCrediantials_ReturnsSuccessDataResultOfUserLoginSuccessDto()
        {
            //Arrange
            string userName = "test";
            string password = "testPassword";
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(userName, password);
            //Assert
            result.Should().BeOfType<SuccessDataResult<UserLoginSuccessDto>>();
            result.As<SuccessDataResult<UserLoginSuccessDto>>().Data.Id.Should().Be(userId);
            result.Success.Should().Be(true);
        }

        [Fact]
        public async Task LoginAsync_WithInValidUserName_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string expectedUserName = "test1";
            string password = "testPassword";
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = expectedUserName,//here
                NormalizedUserName = expectedUserName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(userName, password);
            //Assert
            result.Should().BeOfType<ErrorResult>();
        }
        [Fact]
        public async Task LoginAsync_WithInValidPassword_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string password = "testPassword";
            string expectedPassword = "testpassword1";
            HashingHelper.CreatePasswordHash(expectedPassword, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authService.LoginAsync(userName, password);
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
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = true,
                TemporaryPassword = tempPassword,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //act
            var result = await authService.LoginAsync(userName, tempPassword);
            //arrange
            result.Should().BeOfType<SuccessDataResult<LoginNewPasswordDto>>();
            result.As<SuccessDataResult<LoginNewPasswordDto>>().Data.UserId.Should().Be(userId.ToString());
        }

        [Fact]
        public async Task LogicAsync_InValidCredentialUserNeedsToTakeNewPassword_ReturnsErrorResult()
        {
            //arrange
            string userName = "test";
            string password = "testpassword";
            string tempPassword = "tempPassword";
            string wrongTempPassword = "wrongTempPassword";
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = true,
                TemporaryPassword = tempPassword,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
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
            HashingHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                IsActive = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //act
            var result = await authService.LoginAsync(userName, password);
            //arrange
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.UserIsInactive);
        }

        [Fact]
        public async Task AssignNewPassword_WithValidGuid_ReturnsSuccessResult()
        {
            // Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser
            {
                Id = userId,
                NeedsTakeNewPassword = false
            };
            integrationDbContextMock.Setup(c => c.Users.FindAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await authService.AssignNewPassword(password, userId.ToString());

            // Assert
            result.Should().BeOfType<SuccessResult>();
            result.Success.Should().BeTrue();
            integrationDbContextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            user.NeedsTakeNewPassword.Should().BeTrue();
            user.TemporaryPassword.Should().Be(password);
        }
        [Fact]
        public async Task AssignNewPassword_WithInvalidUserId_ReturnsErrorResult()
        {
            // Arrange
            string password = "newPassword";
            string invalidUserId = "invalidUserId";

            // Act
            var result = await authService.AssignNewPassword(password, invalidUserId);

            // Assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            integrationDbContextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task AssignNewPassword_WithNonExistingUser_ReturnsErrorResult()
        {
            // Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            integrationDbContextMock.Setup(c => c.Users.FindAsync(userId)).ReturnsAsync(null as ApplicationUser);

            // Act
            var result = await authService.AssignNewPassword(password, userId.ToString());

            // Assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            integrationDbContextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CreatePassword_WithValidPassword_ReturnsSuccessResult()
        {
            //Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = "username",
                NormalizedUserName = "username".ToUpperInvariant(),
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            integrationDbContextMock.Setup(db => db.Users.FindAsync(new object[] { userId }, default)).ReturnsAsync(user);
            //act
            var result = await authService.CreatePassword(password, userId, default);
            //assert
            result.Should().BeOfType<SuccessResult>();
            result.Message.Should().Be(Messages.FirstPasswordAssigned);
            user.PasswordHash.Should().NotBeNull();
            user.PasswordSalt.Should().NotBeNull();
            user.NeedsTakeNewPassword.Should().BeFalse();
            integrationDbContextMock.Verify(ctx => ctx.SaveChangesAsync(default), Times.Once);
        }
        [Fact]
        public async Task CreatePassword_WithNotExistingUser_ReturnErrorResult()
        {
            //Arrange
            string password = "newPassword";
            Guid userId = Guid.NewGuid();
            integrationDbContextMock.Setup(db => db.Users.FindAsync(new object[] { userId }, default)).Returns(null!);
            //act
            var result = await authService.CreatePassword(password, userId, default);
            //assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.UserNotFound);
            integrationDbContextMock.Verify(ctx => ctx.SaveChangesAsync(default), Times.Never);
        }


        [Theory]
        [InlineData("")]
        [InlineData(null!)]
        public async Task CreatePassword_WithInvalidPassword_ReturnsErrorResult(string? password)
        {
            //act
            var result = await authService.CreatePassword(password, default, default);
            //assert
            result.Should().BeOfType<ErrorResult>();
            result.Message.Should().Be(Messages.ProcessFailed);
            integrationDbContextMock.Verify(ctx => ctx.SaveChangesAsync(default), Times.Never);
        }
    }
}
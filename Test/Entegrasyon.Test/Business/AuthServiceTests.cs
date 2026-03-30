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
            // Now using bcrypt — legacy fields are cleared
            user.BcryptPasswordHash.Should().NotBeNullOrEmpty();
            user.PasswordHashVersion.Should().Be(1);
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

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsRolesWithRoleClaims()
        {
            // Arrange
            string userName = "test", password = "testpassword";
            var roleClaims = new List<RolesClaims>
            {
                new() { RoleId = 1, Permission = "Products.Read" },
                new() { RoleId = 1, Permission = "Products.Write" }
            };
            var role = new Role { Id = 1, Name = "Admin", RoleClaims = roleClaims };
            ApplicationUser user = CreateUser(userName, password);
            user.Roles = new List<Role> { role };
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            // Act
            var result = await authService.LoginAsync(userName, password);

            // Assert
            result.Should().BeOfType<SuccessDataResult<UserLoginSuccessDto>>();
            var dto = result.As<SuccessDataResult<UserLoginSuccessDto>>().Data;
            dto.Roles.Should().HaveCount(1);
            dto.Roles.First().RoleClaims.Should().HaveCount(2);
            dto.Roles.First().RoleClaims.Should().Contain(rc => rc.Permission == "Products.Read");
            dto.Roles.First().RoleClaims.Should().Contain(rc => rc.Permission == "Products.Write");
        }

        // ──────────────────────────────────────────────────────────────
        // bcrypt Hash Migration Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void CreateBcryptHash_Returns_ValidBcryptString()
        {
            var hash = HashingHelper.CreateBcryptHash("mypassword");
            hash.Should().StartWith("$2");
            hash.Length.Should().BeGreaterThan(20);
        }

        [Fact]
        public void VerifyBcryptHash_WithCorrectPassword_ReturnsTrue()
        {
            var hash = HashingHelper.CreateBcryptHash("correct");
            HashingHelper.VerifyBcryptHash("correct", hash).Should().BeTrue();
        }

        [Fact]
        public void VerifyBcryptHash_WithWrongPassword_ReturnsFalse()
        {
            var hash = HashingHelper.CreateBcryptHash("correct");
            HashingHelper.VerifyBcryptHash("wrong", hash).Should().BeFalse();
        }

        [Fact]
        public async Task LoginAsync_BcryptUser_VerifiesCorrectly()
        {
            // Arrange
            string userName = "testuser", password = "securepass";
            var user = CreateBcryptUser(userName, password);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            // Act
            var result = await authService.LoginAsync(userName, password);

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<SuccessDataResult<UserLoginSuccessDto>>();
        }

        [Fact]
        public async Task LoginAsync_LegacyUser_AutoMigratesToBcrypt()
        {
            // Arrange — user with HMACSHA512 hash (version 0)
            string userName = "legacyuser", password = "legacypass";
            var user = CreateUser(userName, password);  // version 0 by default
            user.PasswordHashVersion = 0;
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            // Act
            var result = await authService.LoginAsync(userName, password);

            // Assert — login success AND migration happened
            result.Success.Should().BeTrue();
            user.PasswordHashVersion.Should().Be(1);
            user.BcryptPasswordHash.Should().NotBeNullOrEmpty();
            user.PasswordHash.Should().BeNull();
            user.PasswordSalt.Should().BeNull();
            mockIntegrationDbContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        // ──────────────────────────────────────────────────────────────
        // Account Lockout Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_SuccessfulLogin_ResetFailedCount()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.FailedLoginCount = 3;
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, password);

            result.Success.Should().BeTrue();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_4Failures_NoLockout()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.FailedLoginCount = 3; // 4th failure will be attempted
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, "wrongpass");

            result.Success.Should().BeFalse();
            user.FailedLoginCount.Should().Be(4);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_5Failures_AccountLocked()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.FailedLoginCount = 4; // 5th failure triggers lock
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, "wrongpass");

            result.Success.Should().BeFalse();
            user.FailedLoginCount.Should().Be(5);
            user.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task LoginAsync_LockedAccount_ReturnsLockedError()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.FailedLoginCount = 5;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10); // still locked
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, password);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("kilitli");
        }

        [Fact]
        public async Task LoginAsync_ExpiredLockout_LoginSucceeds()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.FailedLoginCount = 5;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1); // expired
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, password);

            result.Success.Should().BeTrue();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        // ──────────────────────────────────────────────────────────────
        // Password Reset Token Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task RequestPasswordResetAsync_ValidEmail_GeneratesToken()
        {
            string userName = "user", password = "pass", email = "user@test.com";
            var user = CreateUser(userName, password);
            user.Email = email;
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.RequestPasswordResetAsync(email);

            result.Success.Should().BeTrue();
            user.PasswordResetToken.Should().NotBeNullOrEmpty();
            user.PasswordResetTokenExpiresAt.Should().NotBeNull();
            user.PasswordResetTokenExpiresAt!.Value.Should().BeCloseTo(
                DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
            mockIntegrationDbContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_UnknownEmail_ReturnsSuccessWithoutLeak()
        {
            IList<ApplicationUser> users = [];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            // Should not reveal whether email exists
            var result = await authService.RequestPasswordResetAsync("notexist@test.com");

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_Succeeds()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.PasswordResetToken = "valid-token";
            user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.ResetPasswordAsync("valid-token", "newPassword123");

            result.Success.Should().BeTrue();
            user.PasswordResetToken.Should().BeNull();
            user.PasswordResetTokenExpiresAt.Should().BeNull();
            user.PasswordHashVersion.Should().Be(1);
            user.BcryptPasswordHash.Should().NotBeNullOrEmpty();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_Fails()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.PasswordResetToken = "expired-token";
            user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5); // expired
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.ResetPasswordAsync("expired-token", "newpass");

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("süresi");
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_Fails()
        {
            IList<ApplicationUser> users = [];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.ResetPasswordAsync("nonexistent-token", "newpass");

            result.Success.Should().BeFalse();
        }

        // ──────────────────────────────────────────────────────────────
        // 2FA Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task SetupTwoFactorAsync_ReturnsSetupDto()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            mockIntegrationDbContext.Setup(c => c.Users.FindAsync(new object[] { user.Id }, default))
                .ReturnsAsync(user);

            var result = await authService.SetupTwoFactorAsync(user.Id);

            result.Success.Should().BeTrue();
            result.As<SuccessDataResult<TwoFactorSetupDto>>().Data.Secret.Should().NotBeNullOrEmpty();
            result.As<SuccessDataResult<TwoFactorSetupDto>>().Data.QrCodeUri.Should().Contain("otpauth://totp");
            user.TwoFactorSecret.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_With2FAActive_ReturnsRequiresTwoFactor()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.IsTwoFactorAuthActive = true;
            user.TwoFactorSecret = OtpNet.Base32Encoding.ToString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(20));
            IList<ApplicationUser> users = [user];
            mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(users);

            var result = await authService.LoginAsync(userName, password);

            result.Should().BeOfType<SuccessDataResult<TwoFactorRequiredDto>>();
        }

        [Fact]
        public async Task VerifyTwoFactorAsync_WithValidCode_Succeeds()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            var secretBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(20);
            user.TwoFactorSecret = OtpNet.Base32Encoding.ToString(secretBytes);
            user.IsTwoFactorAuthActive = true;
            mockIntegrationDbContext.Setup(c => c.Users.FindAsync(new object[] { user.Id }, default))
                .ReturnsAsync(user);

            // Generate a valid TOTP code
            var totp = new OtpNet.Totp(secretBytes, step: 30, totpSize: 6);
            var code = totp.ComputeTotp();

            var result = await authService.VerifyTwoFactorAsync(user.Id, code);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GenerateRecoveryCodesAsync_Returns10Codes()
        {
            string userName = "user", password = "pass";
            var user = CreateUser(userName, password);
            user.IsTwoFactorAuthActive = true;
            mockIntegrationDbContext.Setup(c => c.Users.FindAsync(new object[] { user.Id }, default))
                .ReturnsAsync(user);

            var result = await authService.GenerateRecoveryCodesAsync(user.Id);

            result.Success.Should().BeTrue();
            result.As<SuccessDataResult<List<string>>>().Data.Should().HaveCount(10);
            user.TwoFactorRecoveryCodes.Should().NotBeNullOrEmpty();
        }

        // ──────────────────────────────────────────────────────────────
        // Helper methods
        // ──────────────────────────────────────────────────────────────

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
                PasswordHashVersion = 0,
                NeedsTakeNewPassword = needsToTakePassword,
                TemporaryPassword = tempPassword,
                IsActive = isActive,
                Roles = new List<Role>()
            };
        }

        private ApplicationUser CreateBcryptUser(string userName, string password, bool isActive = true)
        {
            return new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Surname = "User",
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                BcryptPasswordHash = HashingHelper.CreateBcryptHash(password),
                PasswordHashVersion = 1,
                IsActive = isActive,
                Roles = new List<Role>()
            };
        }
    }
}
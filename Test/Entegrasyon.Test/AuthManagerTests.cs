using Entegrasyon.Entity.User;
using Shared.Security;
using Moq.EntityFrameworkCore;
using Entegrasyon.Business.Concrete.Auth;
using Moq;
using Entegrasyon.Business.Concrete;
using FluentAssertions;
using Shared.Results;

namespace Entegrasyon.UnitTest
{
    public class AuthManagerTests : BaseTest
    {
        private readonly AuthService authManager;

        public AuthManagerTests()
        {
            authManager = new AuthService(integrationDbContextMock.Object,applicationLoggerMock.Object);
        }

        //[Fact]
        //public void LoginAsync_WithValidCrediantials_ReturnsSuccessDataResultOfUserLoginSuccessDto()
        //{
        //    //Arrange
        //    string userName = "test";
        //    string password = "testPassword";
        //    HashingHelper.CreatePasswordHash(password,out var passwordHash,out var passwordSalt);
        //    Guid userId = Guid.NewGuid();
        //    ApplicationUser user = new ApplicationUser()
        //    {
        //        Id = userId,
        //        Name = "Test",
        //        Surname = "User",
        //        UserName = userName,
        //        PasswordHash = passwordHash,
        //        PasswordSalt = passwordSalt,
        //        NeedsTakeNewPassword = false,
        //        Roles = new List<Role>()
        //    };
        //    IList<ApplicationUser> users = [user];
        //    integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
        //    //Act

        //    //Assert

        //}

        [Fact]
        public async Task LoginAsync_WithInValidUserName_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string password = "testPassword";
            HashingHelper.CreatePasswordHash(password,out var passwordHash,out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName + "1",//here
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authManager.LoginAsync(userName,password);
            //Assert
            result.Should().BeOfType<ErrorResult>();
        }
        [Fact]
        public async Task LoginAsync_WithInValidPassword_ReturnsErrorResult()
        {
            //Arrange
            string userName = "test";
            string password = "testPassword";
            HashingHelper.CreatePasswordHash("thisiswrongpassword",out var passwordHash,out var passwordSalt);
            Guid userId = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Id = userId,
                Name = "Test",
                Surname = "User",
                UserName = userName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                NeedsTakeNewPassword = false,
                Roles = new List<Role>()
            };
            IList<ApplicationUser> users = [user];
            integrationDbContextMock.Setup(c => c.Users).ReturnsDbSet(users);
            //Act
            var result = await authManager.LoginAsync(userName,password);
            //Assert
            result.Should().BeOfType<ErrorResult>();
        }
    }
}
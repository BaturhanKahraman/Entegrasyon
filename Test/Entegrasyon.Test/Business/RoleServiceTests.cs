using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Business;

public class RoleServiceTests : BaseTest
{
    private readonly IRoleService roleService;

    public RoleServiceTests()
    {
        var tenantCache = new TenantMemoryCache(new MemoryCache(new MemoryCacheOptions()), mockTenantContext.Object);
        roleService = new RoleService(mockApplicationLogger.Object, mockContextFactory.Object, tenantCache, new AddRoleDtoValidator(), new EditRoleDtoValidator());
    }

    public static IEnumerable<object[]> invalidMembers => new List<object[]>() {
        new object[]{new AddRoleDto(default!,default!) { Name="",PermissionNames=[]} },
        new object[]{new AddRoleDto(default!,default!) { Name="asd",PermissionNames=[]} },
        new object[]{new AddRoleDto(default!,default!) { Name="",PermissionNames=["Products.Read"]} }
    };

    [Theory]
    [MemberData(nameof(invalidMembers))]
    ///please see file addroledtovalidatortests.cs
    public async Task AddRole_InvalidDto_ReturnsErrorResult(AddRoleDto dto)
    {
        //act
        var result = await roleService.AddRole(dto, default);
        //assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddRole_SameNameExists_ReturnsErrorResult()
    {
        //arrange
        const string name = "sameName";
        Role role = new Role() { Name = name };
        AddRoleDto dto2 = new AddRoleDto(name, ["Products.Read", "Products.Write"]);
        IList<Role> roles = [role];
        mockIntegrationDbContext.Setup(x => x.Roles).ReturnsDbSet(roles);
        //act
        var result = await roleService.AddRole(dto2, default);
        //assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(Messages.RoleExits);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(default), Times.Never);
        mockIntegrationDbContext.Verify(x => x.Roles.AddAsync(It.IsAny<Role>(), default), Times.Never);
    }

    [Fact]
    public async Task AddRole_WithValidDto_AddsAndReturnsSuccessResult()
    {
        //arrange
        const string RoleName = "Role 1";
        AddRoleDto dto = new AddRoleDto(RoleName, ["Products.Read", "Products.Write"]);
        IList<Role> roles = [];
        mockIntegrationDbContext.Setup(x => x.Roles).ReturnsDbSet(roles);
        //act
        var result = await roleService.AddRole(dto);
        //assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.RoleAdded);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        mockIntegrationDbContext.Verify(x => x.Roles.AddAsync(It.IsAny<Role>(), default), Times.Once);
    }

    [Fact]
    public async Task AddRole_WithPermissions_SavesRoleClaimsToDatabase()
    {
        //arrange
        const string RoleName = "Editor";
        var permissions = new List<string> { "Products.Read", "Products.Write", "Categories.Read" };
        AddRoleDto dto = new AddRoleDto(RoleName, permissions);
        IList<Role> roles = [];
        mockIntegrationDbContext.Setup(x => x.Roles).ReturnsDbSet(roles);

        Role? capturedRole = null;
        mockIntegrationDbContext.Setup(x => x.Roles.AddAsync(It.IsAny<Role>(), default))
            .Callback<Role, CancellationToken>((r, _) => capturedRole = r)
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Role>)null!);

        //act
        var result = await roleService.AddRole(dto);

        //assert
        result.Success.Should().BeTrue();
        capturedRole.Should().NotBeNull();
        capturedRole!.RoleClaims.Should().HaveCount(3);
        capturedRole.RoleClaims.Select(rc => rc.Permission).Should()
            .BeEquivalentTo(permissions);
    }

    public static IEnumerable<object[]> invalidEditDtoMembers => new List<object[]>() {
        new object[]{new EditRoleDto(0,"name",["perm1"])},
        new object[]{new EditRoleDto(1,"",["perm1"])},
        new object[]{new EditRoleDto(1,"name",[])},
        new object[]{new EditRoleDto(0,"",["perm1"])},
        new object[]{new EditRoleDto(1,"",[])},
        new object[]{new EditRoleDto(0,"name",[])},
        new object[]{new EditRoleDto(0,"",[])}
    };

    [Theory]
    [MemberData(nameof(invalidEditDtoMembers))]
    public async Task EditRole_WithInvalidDto_ReturnsErrorResult(EditRoleDto dto)
    {
        //arrange
        //act
        var result = await roleService.UpdateRole(dto, default);
        //assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeEmpty();
        mockIntegrationDbContext.Verify(ctx => ctx.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateRole_WithNewPermissions_ReplacesExistingRoleClaims()
    {
        //arrange
        const int roleId = 1;
        const string roleName = "Editor";
        var oldClaims = new List<RolesClaims>
        {
            new() { RoleId = roleId, Permission = "Products.Read" },
            new() { RoleId = roleId, Permission = "Products.Write" }
        };
        var dbRole = new Role
        {
            Id = roleId,
            Name = "OldName",
            NormalizedName = "OLDNAME",
            RoleClaims = oldClaims
        };
        var newPermissions = new List<string> { "Categories.Read", "Categories.Write", "Users.View" };
        var dto = new EditRoleDto(roleId, roleName, newPermissions);

        IList<Role> roles = [dbRole];
        mockIntegrationDbContext.Setup(x => x.Roles).ReturnsDbSet(roles);

        //act
        var result = await roleService.UpdateRole(dto);

        //assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.RoleUpdated);
        dbRole.Name.Should().Be(roleName);
        dbRole.RoleClaims.Should().HaveCount(3);
        dbRole.RoleClaims.Select(rc => rc.Permission).Should()
            .BeEquivalentTo(newPermissions);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}

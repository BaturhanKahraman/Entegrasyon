using System.Security.Claims;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Authorization;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantFeatureAuthorizationHandlerTests
{
    private static ClaimsPrincipal CreateUser(string[] roles, string[] permissions)
    {
        var claims = new List<Claim>();
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("Permission", p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task HandleAsync_FeatureEnabled_UserHasPermission_Succeeds()
    {
        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync("Permissions.Products.View"))
            .ReturnsAsync(true);

        var handler = new TenantFeatureAuthorizationHandler(mockFeatureService.Object);
        var requirement = new PermissionRequirement("Permissions.Products.View");
        var user = CreateUser([], ["Permissions.Products.View"]);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_FeatureDisabled_UserHasPermission_Fails()
    {
        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync("Permissions.Marketplace.View"))
            .ReturnsAsync(false);

        var handler = new TenantFeatureAuthorizationHandler(mockFeatureService.Object);
        var requirement = new PermissionRequirement("Permissions.Marketplace.View");
        var user = CreateUser([], ["Permissions.Marketplace.View"]);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_FeatureEnabled_UserLacksPermission_DoesNotSucceed()
    {
        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync("Permissions.Products.View"))
            .ReturnsAsync(true);

        var handler = new TenantFeatureAuthorizationHandler(mockFeatureService.Object);
        var requirement = new PermissionRequirement("Permissions.Products.View");
        var user = CreateUser([], []); // no permissions
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_FeatureEnabled_AdminRole_Succeeds()
    {
        var mockFeatureService = new Mock<IFeatureService>();
        mockFeatureService.Setup(x => x.IsFeatureEnabledAsync("Permissions.Products.View"))
            .ReturnsAsync(true);

        var handler = new TenantFeatureAuthorizationHandler(mockFeatureService.Object);
        var requirement = new PermissionRequirement("Permissions.Products.View");
        var user = CreateUser(["Admin"], []);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}

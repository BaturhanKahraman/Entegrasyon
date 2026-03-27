using Bunit.TestDoubles;
using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Blazor.Components.Shared;
using Entegrasyon.Business.Tenants;
using MudBlazor.Services;

namespace Entegrasyon.BunitTest.Components.Shared;

public class PermissionGuardedNavLinkTests : TestContext
{
    private readonly Mock<IFeatureService> _mockFeatureService = new();
    private readonly TestAuthorizationContext _authContext;

    public PermissionGuardedNavLinkTests()
    {
        // Register all services BEFORE any component render
        Services.AddMudServices();
        Services.AddSingleton(_mockFeatureService.Object);
        _authContext = this.AddTestAuthorization();

        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void UserWithPermission_RendersNavLink()
    {
        // Arrange: Feature enabled + user has permission
        _mockFeatureService
            .Setup(f => f.IsFeatureEnabledAsync(AppPermissions.Products.View))
            .ReturnsAsync(true);

        _authContext.SetAuthorized("testuser");
        _authContext.SetPolicies(AppPermissions.Products.View);

        // Act
        var cut = RenderComponent<PermissionGuardedNavLink>(parameters => parameters
            .Add(p => p.Permission, AppPermissions.Products.View)
            .Add(p => p.Href, "/products")
            .Add(p => p.Icon, "test-icon")
            .Add(p => p.Title, "Urunler"));

        // Assert: A MudNavLink should be rendered with the title
        var markup = cut.Markup;
        markup.Should().Contain("Urunler");
        markup.Should().Contain("/products");
    }

    [Fact]
    public void UserWithoutPermission_ShowWhenDisabledFalse_RendersNothing()
    {
        // Arrange: Feature enabled but user does NOT have permission
        _mockFeatureService
            .Setup(f => f.IsFeatureEnabledAsync(AppPermissions.Products.View))
            .ReturnsAsync(true);

        _authContext.SetAuthorized("testuser");
        // No policies set -> AuthorizeView will show NotAuthorized

        // Act
        var cut = RenderComponent<PermissionGuardedNavLink>(parameters => parameters
            .Add(p => p.Permission, AppPermissions.Products.View)
            .Add(p => p.Href, "/products")
            .Add(p => p.Icon, "test-icon")
            .Add(p => p.Title, "Urunler")
            .Add(p => p.ShowWhenDisabled, false));

        // Assert: Title should not appear
        cut.Markup.Should().NotContain("Urunler");
    }

    [Fact]
    public void FeatureDisabled_ShowWhenDisabledTrue_RendersDisabledNavLink()
    {
        // Arrange: Feature is NOT enabled for tenant, ShowWhenDisabled = true
        _mockFeatureService
            .Setup(f => f.IsFeatureEnabledAsync(AppPermissions.Products.View))
            .ReturnsAsync(false);

        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<PermissionGuardedNavLink>(parameters => parameters
            .Add(p => p.Permission, AppPermissions.Products.View)
            .Add(p => p.Href, "/products")
            .Add(p => p.Icon, "test-icon")
            .Add(p => p.Title, "Urunler")
            .Add(p => p.ShowWhenDisabled, true));

        // Assert: Should show disabled nav link with title
        var markup = cut.Markup;
        markup.Should().Contain("Urunler");
        markup.Should().Contain("disabled", because: "the link should be visually disabled");
    }
}

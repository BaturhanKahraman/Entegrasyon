using System.Reflection;
using Entegrasyon.ApplicationBootstrap.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Entegrasyon.Test.Security;

/// <summary>
/// Verifies that all routable pages have the correct [Authorize(Policy = "...")] attribute.
/// Auth pages (Login, PasswordReset) and Dashboard are exceptions.
/// </summary>
public class PagePermissionAttributeTests
{
    private static readonly Assembly BlazorAssembly =
        typeof(Entegrasyon.Blazor.Features.Dashboard.Index).Assembly;

    /// <summary>
    /// Gets all routable page components (those with @page / [RouteAttribute]).
    /// </summary>
    private static IEnumerable<Type> GetAllPageComponents()
    {
        return BlazorAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes<RouteAttribute>().Any())
            .Where(t => !t.IsAbstract);
    }

    /// <summary>
    /// Pages that should NOT have a policy-based Authorize attribute.
    /// Auth pages use [AllowAnonymous] or no policy. Dashboard is open to all authenticated users.
    /// </summary>
    private static readonly HashSet<string> ExemptPages = new()
    {
        "Entegrasyon.Blazor.Features.Auth.Login",
        "Entegrasyon.Blazor.Features.Auth.PasswordReset",
        "Entegrasyon.Blazor.Features.Dashboard.Index",
        "Entegrasyon.Blazor.Features.Profile.ProfilePage",
        "Entegrasyon.Blazor.Features.Notifications.NotificationsPage",
        "Entegrasyon.Blazor.Features.Chat.ChatPage",
        "Entegrasyon.Blazor.Pages.Error",
    };

    /// <summary>
    /// Expected permission mapping for each page (by full type name).
    /// </summary>
    private static readonly Dictionary<string, string> ExpectedPermissions = new()
    {
        // Products
        { "Entegrasyon.Blazor.Features.Products.Products", AppPermissions.Products.View },
        { "Entegrasyon.Blazor.Features.Products.AddProduct", AppPermissions.Products.Create },
        { "Entegrasyon.Blazor.Features.Products.ProductDetail", AppPermissions.Products.View },
        { "Entegrasyon.Blazor.Features.Products.ProductEdit", AppPermissions.Products.Edit },

        // Customers
        { "Entegrasyon.Blazor.Features.Customers.Customers", AppPermissions.Customers.View },

        // Categories
        { "Entegrasyon.Blazor.Features.Categories.Categories", AppPermissions.Categories.View },
        { "Entegrasyon.Blazor.Features.Categories.CategoryWizard", AppPermissions.Categories.View },
        { "Entegrasyon.Blazor.Features.CategoryImport.CategoryImport", AppPermissions.Categories.Edit },

        // Attributes
        { "Entegrasyon.Blazor.Features.Attributes.AttributesPage", AppPermissions.Categories.View },

        // Brands
        { "Entegrasyon.Blazor.Features.Brands.BrandsPage", AppPermissions.Brands.View },

        // BranchOffices
        { "Entegrasyon.Blazor.Features.BranchOffices.BranchOfficesPage", AppPermissions.BranchOffices.View },
        { "Entegrasyon.Blazor.Features.BranchOffices.BranchOfficeDetail", AppPermissions.BranchOffices.View },

        // Sales & POS
        { "Entegrasyon.Blazor.Features.POS.POSPage", AppPermissions.Sales.Create },
        { "Entegrasyon.Blazor.Features.Sales.Sales", AppPermissions.Sales.View },

        // Orders
        { "Entegrasyon.Blazor.Features.Orders.OrdersPage", AppPermissions.Orders.View },
        { "Entegrasyon.Blazor.Features.Orders.MarketplaceOrders", AppPermissions.Orders.View },
        { "Entegrasyon.Blazor.Features.Orders.OrderDetail", AppPermissions.Orders.View },
        { "Entegrasyon.Blazor.Features.Orders.InvoicesPage", AppPermissions.Orders.View },

        // Invoicing
        { "Entegrasyon.Blazor.Features.Invoicing.InvoicesPage", AppPermissions.Orders.View },

        // Shipping
        { "Entegrasyon.Blazor.Features.Shipping.ShipmentTrackingPage", AppPermissions.Cargo.View },

        // BulkOperations
        { "Entegrasyon.Blazor.Features.BulkOperations.BulkOperationsPage", AppPermissions.Products.Edit },

        // Reports
        { "Entegrasyon.Blazor.Features.Reports.SalesReport", AppPermissions.Reports.View },
        { "Entegrasyon.Blazor.Features.Reports.ProfitLossReport", AppPermissions.Reports.View },
        { "Entegrasyon.Blazor.Features.Reports.ProductPerformanceReport", AppPermissions.Reports.View },
        { "Entegrasyon.Blazor.Features.Reports.StockAlertsReport", AppPermissions.Reports.View },
        { "Entegrasyon.Blazor.Features.Reports.InventoryReport", AppPermissions.Reports.View },
        { "Entegrasyon.Blazor.Features.Reports.MarketplaceReport", AppPermissions.Reports.View },

        // Marketplace Sync
        { "Entegrasyon.Blazor.Features.MarketplaceSync.SyncOverview", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.CategorySync", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync.AttributeSyncPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.BrandMappingPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch.BulkCategoryMatchPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.CommissionRates.CommissionRatesPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync.ProductSyncPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync.ProductSyncDetailPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync.ProductSyncFullDetailPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync.BulkProductSyncPage", AppPermissions.Marketplace.View },
        { "Entegrasyon.Blazor.Features.MatchedEntityImport.MatchedEntityImport", AppPermissions.Marketplace.View },

        // Users & Roles
        { "Entegrasyon.Blazor.Features.Users.Users", AppPermissions.Users.View },
        { "Entegrasyon.Blazor.Features.Users.UserEdit", AppPermissions.Users.View },
        { "Entegrasyon.Blazor.Features.Admin.RoleManagement", AppPermissions.Roles.View },

        // Settings
        { "Entegrasyon.Blazor.Features.Settings.GeneralSettings", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Settings.NotificationSettings", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Settings.IntegrationSettings", AppPermissions.Integrations.View },
        { "Entegrasyon.Blazor.Features.Settings.PrinterSettings", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Settings.DesktopAppSettings", AppPermissions.Settings.View },

        // Notifications Admin
        { "Entegrasyon.Blazor.Features.Notifications.AdminNotifications", AppPermissions.Notifications.Manage },

        // Storefront
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontSettingsPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontLegalPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontBannersPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontPaymentPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontReviewsPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontReturnsPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontMessagesPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontNewsletterPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontSizeGuidesPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontCampaignsPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontEmailCampaignsPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontSellersPage", AppPermissions.Settings.View },
        { "Entegrasyon.Blazor.Features.Storefront.StorefrontPayoutsPage", AppPermissions.Settings.View },
    };

    [Fact]
    public void AllPages_ShouldHaveCorrectPermissionAttribute()
    {
        var pages = GetAllPageComponents().ToList();
        var missingPages = new List<string>();
        var wrongPolicyPages = new List<string>();

        foreach (var page in pages)
        {
            var fullName = page.FullName!;

            if (ExemptPages.Contains(fullName))
                continue;

            if (!ExpectedPermissions.TryGetValue(fullName, out var expectedPolicy))
            {
                missingPages.Add($"No expected permission mapping for: {fullName}");
                continue;
            }

            var authorizeAttr = page.GetCustomAttributes<AuthorizeAttribute>().ToList();
            var policyAttr = authorizeAttr.FirstOrDefault(a => !string.IsNullOrEmpty(a.Policy));

            if (policyAttr is null)
            {
                wrongPolicyPages.Add($"{fullName}: expected [{expectedPolicy}] but has no policy-based [Authorize]");
                continue;
            }

            if (policyAttr.Policy != expectedPolicy)
            {
                wrongPolicyPages.Add($"{fullName}: expected [{expectedPolicy}] but found [{policyAttr.Policy}]");
            }
        }

        var allErrors = missingPages.Concat(wrongPolicyPages).ToList();
        allErrors.Should().BeEmpty(
            "all routable pages should have correct permission policy attributes.\n" +
            string.Join("\n", allErrors));
    }

    [Fact]
    public void AuthPages_ShouldNotRequireSpecificPermission()
    {
        var loginPage = GetAllPageComponents()
            .FirstOrDefault(t => t.FullName == "Entegrasyon.Blazor.Features.Auth.Login");

        loginPage.Should().NotBeNull();

        var authorizeAttr = loginPage!.GetCustomAttributes<AuthorizeAttribute>()
            .FirstOrDefault(a => !string.IsNullOrEmpty(a.Policy));

        authorizeAttr.Should().BeNull("auth pages should not require a specific permission policy");
    }

    [Fact]
    public void DashboardPage_ShouldNotRequireSpecificPermission()
    {
        var dashboardPage = GetAllPageComponents()
            .FirstOrDefault(t => t.FullName == "Entegrasyon.Blazor.Features.Dashboard.Index");

        dashboardPage.Should().NotBeNull();

        var authorizeAttr = dashboardPage!.GetCustomAttributes<AuthorizeAttribute>()
            .FirstOrDefault(a => !string.IsNullOrEmpty(a.Policy));

        authorizeAttr.Should().BeNull("dashboard should be accessible to all authenticated users");
    }
}

using Entegrasyon.Blazor.Middleware;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Entegrasyon.UnitTest.Tenants;

public class BlazorTenantResolutionMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(string host, string path = "/", Dictionary<string, string?>? configValues = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(mockEnv.Object);
        services.AddSingleton<IConfiguration>(config);
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_InitializesTenantContext()
    {
        var tenantEntry = new TenantRegistryEntry(
            5, "acme", "Acme Corp", "Host=localhost;Database=tenant_5", true, "Standard");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("acme"))
            .ReturnsAsync(tenantEntry);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        tenantContext.IsInitialized.Should().BeTrue();
        tenantContext.TenantId.Should().Be(5);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownSubdomain_Returns404()
    {
        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync(It.IsAny<string>()))
            .ReturnsAsync((TenantRegistryEntry?)null);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("unknown.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        httpContext.Response.StatusCode.Should().Be(404);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithInactiveTenant_Returns404()
    {
        var tenantEntry = new TenantRegistryEntry(
            5, "acme", "Acme Corp", "conn", false, "Standard");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("acme"))
            .ReturnsAsync(tenantEntry);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        httpContext.Response.StatusCode.Should().Be(404);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Localhost_InDevelopment_FallsBackToDevTenant()
    {
        var devTenantEntry = new TenantRegistryEntry(
            1, "dev", "Development Tenant", "Host=localhost;Database=dev", true, "Pro");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("dev"))
            .ReturnsAsync(devTenantEntry);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("localhost");

        // Set up IWebHostEnvironment as Development
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(mockEnv.Object);
        services.AddSingleton<IConfiguration>(config);
        httpContext.RequestServices = services.BuildServiceProvider();

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        tenantContext.IsInitialized.Should().BeTrue();
        tenantContext.TenantId.Should().Be(1);
        nextCalled.Should().BeTrue();
        mockRegistry.Verify(x => x.GetBySubdomainAsync("dev"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Localhost_InProduction_Returns404()
    {
        var mockRegistry = new Mock<ITenantRegistry>();
        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("localhost");

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        httpContext.Response.StatusCode.Should().Be(404);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithDefaultSubdomainConfig_UsesConfiguredTenant()
    {
        var stageTenant = new TenantRegistryEntry(
            2, "stage", "Stage Tenant", "Host=localhost;Database=stage", true, "Standard");

        var mockRegistry = new Mock<ITenantRegistry>();
        mockRegistry.Setup(x => x.GetBySubdomainAsync("stage"))
            .ReturnsAsync(stageTenant);

        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("192.168.1.78", configValues: new()
        {
            ["Tenant:DefaultSubdomain"] = "stage"
        });

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        tenantContext.IsInitialized.Should().BeTrue();
        tenantContext.TenantId.Should().Be(2);
        nextCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData("/_blazor")]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/css/site.css")]
    [InlineData("/js/app.js")]
    [InlineData("/favicon.ico")]
    public async Task InvokeAsync_WithStaticPath_SkipsResolution(string path)
    {
        var mockRegistry = new Mock<ITenantRegistry>();
        var tenantContext = new HttpTenantContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new BlazorTenantResolutionMiddleware(next);
        var httpContext = CreateHttpContext("acme.app.entegrasyon.com", path);

        await middleware.InvokeAsync(httpContext, mockRegistry.Object, tenantContext);

        nextCalled.Should().BeTrue();
        mockRegistry.Verify(x => x.GetBySubdomainAsync(It.IsAny<string>()), Times.Never);
    }
}

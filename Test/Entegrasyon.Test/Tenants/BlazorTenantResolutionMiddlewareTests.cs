using Entegrasyon.Blazor.Middleware;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Entegrasyon.UnitTest.Tenants;

public class BlazorTenantResolutionMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(string host, string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
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

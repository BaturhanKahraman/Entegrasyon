using System.Security.Claims;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.ApiKeys;
using Entegrasyon.Entity.Devices;
using Entegrasyon.MVC.Infrastructure.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Entegrasyon.UnitTest.Security;

public class ApiKeyAuthenticationMiddlewareTests
{
    private readonly Mock<IApiKeyManager> apiKeyManager = new();
    private readonly Mock<IDeviceManager> deviceManager = new();

    private ApiKeyAuthenticationMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new ApiKeyAuthenticationMiddleware(next);
    }

    private async Task<HttpContext> Invoke(string path, string? authHeader = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        if (authHeader is not null)
            ctx.Request.Headers.Authorization = authHeader;
        ctx.Response.Body = new MemoryStream();

        var middleware = CreateMiddleware();
        await middleware.InvokeAsync(ctx, apiKeyManager.Object, deviceManager.Object);
        return ctx;
    }

    [Fact]
    public async Task NonApiPath_PassesThrough()
    {
        var ctx = await Invoke("/products");

        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RegisterPath_BypassesAuth()
    {
        var ctx = await Invoke("/api/device/register");

        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MissingAuthHeader_Returns401()
    {
        var ctx = await Invoke("/api/print/batch");

        ctx.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task BearerEntKey_Valid_SetsApiKeyClaims()
    {
        apiKeyManager.Setup(m => m.ValidateKeyAsync("ent_valid"))
            .ReturnsAsync(new ApiKey { Id = 42, TenantId = 7, Scopes = "[\"read\"]" });

        var ctx = await Invoke("/api/something", "Bearer ent_valid");

        ctx.Response.StatusCode.Should().Be(200);
        ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
        ctx.User.FindFirst("ApiKeyId")!.Value.Should().Be("42");
        ctx.User.FindFirst("TenantId")!.Value.Should().Be("7");
    }

    [Fact]
    public async Task BearerEntKey_Invalid_Returns401()
    {
        apiKeyManager.Setup(m => m.ValidateKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((ApiKey?)null);

        var ctx = await Invoke("/api/something", "Bearer ent_invalid");

        ctx.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task BearerDevKey_Valid_SetsDeviceClaims()
    {
        deviceManager.Setup(m => m.ValidateKeyAsync("dev_valid"))
            .ReturnsAsync(new Device { Id = 99, TenantId = 3, Name = "Kasa-1" });

        var ctx = await Invoke("/api/something", "Bearer dev_valid");

        ctx.Response.StatusCode.Should().Be(200);
        ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
        ctx.User.FindFirst("DeviceId")!.Value.Should().Be("99");
        ctx.User.FindFirst("TenantId")!.Value.Should().Be("3");
        ctx.User.FindFirst(ClaimTypes.AuthenticationMethod)!.Value.Should().Be("Device");
    }

    [Fact]
    public async Task BearerDevKey_Invalid_Returns401()
    {
        deviceManager.Setup(m => m.ValidateKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Device?)null);

        var ctx = await Invoke("/api/something", "Bearer dev_unknown");

        ctx.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task BearerUnknownPrefix_Returns401()
    {
        var ctx = await Invoke("/api/something", "Bearer xyz_random_key");

        ctx.Response.StatusCode.Should().Be(401);
    }
}

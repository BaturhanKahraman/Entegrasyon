using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Entegrasyon.MVC.Features.Devices;

public sealed record DeviceRegisterRequest(string InviteCode, string DeviceName, string OS, string Hostname);

public sealed record DeviceRegisterResponse(int DeviceId, int TenantId, string DeviceName, string DeviceApiKey);

public class DeviceController(IDeviceManager deviceManager) : Controller
{
    [HttpPost("/api/device/register")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("device-register")]
    public async Task<IActionResult> Register([FromBody] DeviceRegisterRequest request)
    {
        if (request is null)
            return BadRequest(new { error = "İstek gövdesi eksik." });

        var result = await deviceManager.RegisterWithInviteCodeAsync(
            request.InviteCode, request.DeviceName, request.OS, request.Hostname);

        if (!result.Success)
            return BadRequest(new { error = result.Message });

        var (device, plainKey) = result.Data;
        return Ok(new DeviceRegisterResponse(device.Id, device.TenantId, device.Name, plainKey));
    }
}

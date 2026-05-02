using Entegrasyon.Entity.Devices;

namespace Entegrasyon.MVC.Features.Devices;

public class DesktopSettingsVm
{
    public List<Device> Devices { get; set; } = [];
    public List<DeviceInviteCode> ActiveInviteCodes { get; set; } = [];
    public string? NewlyIssuedInviteCode { get; set; }
}

using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Dtos.Settings;

namespace Entegrasyon.MVC.Features.Settings.ViewModels;

public class PrintingSettingsVm
{
    public List<ApplicationSettingDto> Settings { get; set; } = [];
    public List<LabelTemplateDto> Templates { get; set; } = [];
}

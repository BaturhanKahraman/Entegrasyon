using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Settings;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class SettingMapper
{
    public static partial ApplicationSettingDto MapToDto(ApplicationSetting setting);
    public static partial List<ApplicationSettingDto> MapToDtoList(List<ApplicationSetting> settings);
}

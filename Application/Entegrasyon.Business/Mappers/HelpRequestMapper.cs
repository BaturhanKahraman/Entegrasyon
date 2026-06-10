using Entegrasyon.Entity.Dtos.Help;
using Entegrasyon.Entity.Help;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class HelpRequestMapper
{
    public partial HelpRequest MapToEntity(CreateHelpRequestDto dto);
}

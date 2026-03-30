using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class UserMapper
{
    [MapProperty(nameof(AddUserDto.BranchOfficeId), nameof(ApplicationUser.DefaultBranchOfficeId))]
    public partial ApplicationUser MapToEntity(AddUserDto dto);

    public partial AddUserDto MapToDto(ApplicationUser user);
}

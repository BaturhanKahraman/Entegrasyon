using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class BranchOfficeMapper
{
    public partial BranchOffice MapToEntity(BranchOfficeAddDto dto);
    public partial BranchOffice MapToEntity(BranchOfficeEditDto dto);
}

using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract
{
    public interface IAttributeKeyValueDal : IEntityRepository<AttributeKeyValue>
    {
        Task<IEnumerable<string>> ValidateKeyValues(IEnumerable<AttributeKeyValue> attributeKeyValues);
    }
}

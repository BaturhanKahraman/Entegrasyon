using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager(IDbContextFactory<IntegrationDbContext> contextFactory) : IAttributeKeyValueManager
{
    public void ClearEmptyAttributes(Product product)
    {
        product.AttributeKeyValues = product.AttributeKeyValues
            .Where(x=>(x.AttributeValueId == 0 && !string.IsNullOrEmpty(x.CustomValue) || x.AttributeValueId>0))
            .ToList();
    }

}

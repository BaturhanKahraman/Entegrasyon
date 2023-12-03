using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Concrete;

public sealed class CategoryAttributeValueManager
{
    private readonly ICategoryAttributeValueDal _cavDal;

    public CategoryAttributeValueManager(ICategoryAttributeValueDal cavDal)
    {
        _cavDal = cavDal;
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id)
    {

        throw new NotImplementedException();
    }
}

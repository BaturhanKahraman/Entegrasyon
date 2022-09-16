using Entegrasyon.DataAccess.Abstract;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager
    {
        private readonly ICategoryDal _categoryDal;
        private readonly ApplicationLogManager _applicationLogManager;

        public CategoryManager(ICategoryDal categoryDal,ApplicationLogManager applicationLogManager)
        {
            _categoryDal = categoryDal;
            _applicationLogManager = applicationLogManager;
        }

        public async Task AddCategory() { }
        public async Task UpdateCategory() { }
        public async Task DeleteCategory(int categoryId) { }
        public async Task GetCategoryDetailList() { }
        public async Task GetCategoryDetailById() { }


    }
}

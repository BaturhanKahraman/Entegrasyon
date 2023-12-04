using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.MVC.ViewModels.Category;
using Entegrasyon.MVC.ViewModels.CategoryAttribute;
using Entegrasyon.MVC.ViewModels.Customer;
using Entegrasyon.MVC.ViewModels.Office;
using Entegrasyon.MVC.ViewModels.User;

namespace Entegrasyon.MVC.Utility.Mapper
{
    public class CustomMapProfile:Profile
    {
        public CustomMapProfile()
        {
            #region Categories
            CreateMap<CategoryDetailDto,CategoryDetailListViewModel>();
            CreateMap<Category,CategoryUpsertViewModel>();
            CreateMap<CategoryUpsertViewModel,EditCategoryDto>();
            CreateMap<CategoryUpsertViewModel,AddCategoryDto>()
                .ForCtorParam("CategoryAttributes",x=>Enumerable.Empty<AddCategoryAttributeDto>());
            #endregion
            #region CategoryAttributes
            CreateMap<CategoryAttributeCreateViewModel, AddCategoryAttributeDto>()
                .ForMember(x=>x.CategoryAttributeHumanized,z=>z.Ignore());
            CreateMap<CategoryAttributeValueViewModel, CategoryAttributeValue>();
            #endregion







            #region Customer
            CreateMap<CustomerDetailDto,CustomerDetailListViewModel>();
            CreateMap<CustomerDetailDto,CustomerDetailViewModel>();
            CreateMap<RetailCustomer, CustomerEditViewModel>();
            CreateMap<CorporateCustomer, CustomerEditViewModel>();
            CreateMap<CustomerAddViewModel, CustomerAddDto>()
                .ForMember(d => d.FullAddress, opt => opt.MapFrom(s => s.Address))
                .ForCtorParam("FullAddress", x => x.MapFrom(d => d.Address));

            CreateMap<CustomerEditViewModel, UpdateCustomerDto>();



            #endregion
        }
    }
}

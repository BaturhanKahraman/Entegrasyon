using AutoMapper;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.MVC.ViewModels.Category;
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
            CreateMap<CategoryAddViewModel,AddCategoryDto>()
                .ForCtorParam("CategoryAttributes",x=>Enumerable.Empty<AddCategoryAttributeDto>());
            #endregion
            #region UserMappings
            CreateMap<UserAddViewModel,AddUserDto>();
            CreateMap<ApplicationUser,UserEditViewModel>()
                .ForMember(source => source.BranchOfficeId,opt => opt.MapFrom(dest => dest.DefaultBranchOfficeId))
                .ReverseMap();
            CreateMap<UserEditViewModel,UserEditDto>()
                .ForMember(s => s.DefaultBranchOfficeId,opt => opt.MapFrom(d => d.BranchOfficeId.Value))
                .ForCtorParam(nameof(UserEditDto.DefaultBranchOfficeId),x => x.MapFrom(z => z.BranchOfficeId.Value))
                .ForMember(s => s.RoleId,opt => opt.MapFrom(d => d.RoleId.Value))
                .ForCtorParam(nameof(UserEditDto.DefaultBranchOfficeId),x => x.MapFrom(z => z.RoleId.Value))
                .ReverseMap();
            CreateMap<UserDetailDto,UserDetailViewModel>()
                .ForMember(dest => dest.BranchOfficeName,opt => opt.MapFrom(s => s.DefaultOfficeName));

            #endregion

            #region Office

            CreateMap<BranchDetailDto, OfficeDetailViewModel>().ReverseMap();
            CreateMap<OfficeCreateViewModel,BranchOfficeAddDto>();
            CreateMap<BranchOffice,OfficeEditViewModel>();
            CreateMap<BranchOfficeEditDto,OfficeEditViewModel>().ReverseMap();

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

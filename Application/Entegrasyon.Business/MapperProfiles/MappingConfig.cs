using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Brands.Import;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Matches;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.MapperProfiles;

public static class MappingConfig
{
    public static void AddBusinessMapping(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        // Brand Marketplace Matching (still using Mapster — BrandMatchMapper not yet migrated)
        config.NewConfig<BrandMarketPlaceMatch, BrandMarketPlaceMatchDto>()
              .Map(dest => dest.ApplicationBrandName, src => src.ApplicationBrand.Name)
              .Map(dest => dest.MarketPlaceBrandName, src => ""); // TODO: Get from external API or local cache

        config.NewConfig<CreateBrandMarketPlaceMatchDto, BrandMarketPlaceMatch>();

        // Trendyol (not yet migrated to Mapperly)
        config.NewConfig<TrendyolCategoryAttribute, CategoryAttribute>()
              .Map(dest => dest.CategoryAttributeKey, src => src.Attribute.Name)
              .Map(dest => dest.CategoryAttributeHumanized, src => src.Attribute.Name);

        config.NewConfig<TrendyolBrand, Brand>()
              .Ignore(dest => dest.Id);

        config.NewConfig<TrendyolAttributeValue, CategoryAttributeValue>()
              .Ignore(dest => dest.Id);

        services.AddSingleton(config);
        services.AddMapster();
    }
}

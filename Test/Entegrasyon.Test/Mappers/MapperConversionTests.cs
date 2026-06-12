using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using ProductMapper = Entegrasyon.Business.Mappers.ProductMapper;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Settings;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.User;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Mappers;

public class MapperConversionTests
{
    // ── Task 2: SettingMapper ────────────────────────────────────────────────
    [Fact]
    public void SettingMapper_MapToDto_MapsAllFields()
    {
        var setting = new ApplicationSetting { Key = "TestKey", Value = "TestValue" };
        var dto = SettingMapper.MapToDto(setting);
        dto.Key.Should().Be("TestKey");
        dto.Value.Should().Be("TestValue");
    }

    [Fact]
    public void SettingMapper_MapToDtoList_ReturnsCorrectCount()
    {
        var settings = new List<ApplicationSetting>
        {
            new() { Key = "K1", Value = "V1" },
            new() { Key = "K2", Value = "V2" }
        };
        var dtos = SettingMapper.MapToDtoList(settings);
        dtos.Should().HaveCount(2);
        dtos[0].Key.Should().Be("K1");
    }

    // ── Task 3: CargoCompanyMapper ───────────────────────────────────────────
    [Fact]
    public void CargoCompanyMapper_MapToEntity_MapsFields()
    {
        var mapper = new CargoCompanyMapper();
        var dto = new AddCargoCompanyDto { Name = "DHL" };
        var entity = mapper.MapToEntity(dto);
        entity.Name.Should().Be("DHL");
    }

    // ── Task 4: BranchOfficeMapper ───────────────────────────────────────────
    [Fact]
    public void BranchOfficeMapper_MapAddDtoToEntity_MapsName()
    {
        var mapper = new BranchOfficeMapper();
        var dto = new BranchOfficeAddDto("Istanbul HQ");
        var entity = mapper.MapToEntity(dto);
        entity.Name.Should().Be("Istanbul HQ");
    }

    // ── Task 5: BrandMapper ──────────────────────────────────────────────────
    [Fact]
    public void BrandMapper_MapToEntity_MapsName()
    {
        var mapper = new BrandMapper();
        var dto = new AddBrandDto { Name = "Nike" };
        var entity = mapper.MapToEntity(dto);
        entity.Name.Should().Be("Nike");
    }

    // ── Task 6: SaleMapper ───────────────────────────────────────────────────
    [Fact]
    public void SaleMapper_SaleItemToDto_RemapsDiscountVoucherCode()
    {
        var mapper = new SaleMapper();
        var item = new SaleItem { UsedDiscountVoucherCode = "SAVE10" };
        var dto = mapper.MapToDto(item);
        dto.DiscountVoucherCode.Should().Be("SAVE10");
    }

    [Fact]
    public void SaleMapper_DtoToSaleItem_RemapsDiscountVoucherCode()
    {
        var mapper = new SaleMapper();
        var dto = new SaleItemDto(Guid.Empty, 0, 0, 0m, 0, "SAVE10");
        var item = mapper.MapToEntity(dto);
        item.UsedDiscountVoucherCode.Should().Be("SAVE10");
    }

    // ── Task 11: CategoryMapper ──────────────────────────────────────────────
    [Fact]
    public void CategoryMapper_ZeroSuperCategoryId_MapsToNull()
    {
        var mapper = new CategoryMapper();
        var dto = new AddCategoryDto("Test", [], 0, false);
        var entity = mapper.MapToEntity(dto);
        entity.SuperCategoryId.Should().BeNull();
    }

    [Fact]
    public void CategoryMapper_NonZeroSuperCategoryId_MapsValue()
    {
        var mapper = new CategoryMapper();
        var dto = new AddCategoryDto("Child", [], 5, false);
        var entity = mapper.MapToEntity(dto);
        entity.SuperCategoryId.Should().Be(5);
    }

    [Fact]
    public void CategoryMapper_CategoryAttributes_NotMapped()
    {
        var mapper = new CategoryMapper();
        var dto = new AddCategoryDto("Test", [], 0, false);
        var entity = mapper.MapToEntity(dto);
        entity.CategoryAttributes.Should().BeNullOrEmpty();
    }

    // ── Task 12: CategoryAttributeMapper ────────────────────────────────────
    [Fact]
    public void CategoryAttributeMapper_MapToEntity_IgnoresCategories()
    {
        var mapper = new CategoryAttributeMapper();
        var dto = new AddCategoryAttributeDto { CategoryAttributeKey = "color" };
        var entity = mapper.MapToEntity(dto);
        entity.Categories.Should().BeNullOrEmpty();
    }

    [Fact]
    public void CategoryAttributeMapper_ToJunctionUpdate_BuildsNestedObject()
    {
        var dto = new EditCategoryAttributeDto(
            Id: 7,
            IsRequired: false,
            IsVarianter: false,
            CategoryAttributeKey: "color",
            IsSlicer: false,
            CategoryAttributeHumanized: "Renk",
            CategoryAttributeValues: [],
            CategoryId: 0);
        var junction = CategoryAttributeMapper.ToJunctionUpdate(dto);
        junction.CategoryAttributeId.Should().Be(7);
        junction.CategoryAttribute.Id.Should().Be(7);
        junction.CategoryAttribute.CategoryAttributeHumanized.Should().Be("Renk");
    }

    // ── Task 13: CustomerMapper ──────────────────────────────────────────────
    [Fact]
    public void CustomerMapper_MapToRetail_MapsNationalIdentity()
    {
        var mapper = new CustomerMapper();
        var dto = new CustomerAddDto("12345678901", "", "Ali", "Veli", "", "555", "Istanbul", "Retail");
        var entity = mapper.MapToRetail(dto);
        entity.NationalIdentity.Should().Be("12345678901");
    }

    [Fact]
    public void CustomerMapper_MapToCorporate_MapsTaxNumberAndCorporateName()
    {
        // Yeni create formu kurumsal müşteride vergi no'yu kendi TaxNumber alanında gönderir
        // (eski form NationalIdentity alanına koyup mapper'da TaxNumber'a taşıyordu).
        var mapper = new CustomerMapper();
        var dto = new CustomerAddDto(
            NationalIdentity: "",
            TaxNumber: "1234567890",
            Name: "",
            Surname: "",
            CorporateName: "Acme Ltd",
            PhoneNumber: "555",
            FullAddress: "Istanbul",
            CustomerType: "Corporate");
        var entity = mapper.MapToCorporate(dto);
        entity.TaxNumber.Should().Be("1234567890");
        entity.CorporateName.Should().Be("Acme Ltd");
    }

    // ── Task 7: ProductMapper ────────────────────────────────────────────────
    [Fact]
    public void ProductMapper_VatRateNull_DefaultsToZero()
    {
        var mapper = new ProductMapper();
        var dto = new AddProductVariantDto { VatRate = null, CurrencyType = "TRY", Barcode = "123" };
        var variant = mapper.MapToEntity(dto);
        variant.VatRate.Should().Be(0m);
    }

    [Fact]
    public void ProductMapper_VatRateSet_PreservesValue()
    {
        var mapper = new ProductMapper();
        var dto = new AddProductVariantDto { VatRate = 18m, CurrencyType = "TRY", Barcode = "123" };
        var variant = mapper.MapToEntity(dto);
        variant.VatRate.Should().Be(18m);
    }

    // ── Task 8: UserMapper ───────────────────────────────────────────────────
    [Fact]
    public void UserMapper_AddUserDto_MapsBranchOfficeIdToDefaultBranchOfficeId()
    {
        var mapper = new UserMapper();
        var dto = new AddUserDto { BranchOfficeId = 42, UserName = "test", Email = "t@t.com" };
        var user = mapper.MapToEntity(dto);
        user.DefaultBranchOfficeId.Should().Be(42);
    }

    // ── Task 10: BrandMatchMapper ────────────────────────────────────────────
    [Fact]
    public void BrandMatchMapper_MapToDto_ReadsApplicationBrandName()
    {
        var mapper = new BrandMatchMapper();
        var entity = new BrandMarketPlaceMatch
        {
            ApplicationBrand = new Brand { Name = "Nike" }
        };
        var dto = mapper.MapToDto(entity);
        dto.ApplicationBrandName.Should().Be("Nike");
        dto.MarketPlaceBrandName.Should().BeNull();
    }

    [Fact]
    public void BrandMatchMapper_MapToDto_PreservesMarketPlaceBrandName()
    {
        var mapper = new BrandMatchMapper();
        var entity = new BrandMarketPlaceMatch
        {
            ApplicationBrand = new Brand { Name = "Nike" },
            MarketPlaceBrandId = 12345,
            MarketPlaceBrandName = "Nike (Trendyol)"
        };
        var dto = mapper.MapToDto(entity);
        dto.MarketPlaceBrandId.Should().Be(12345);
        dto.MarketPlaceBrandName.Should().Be("Nike (Trendyol)");
    }
}

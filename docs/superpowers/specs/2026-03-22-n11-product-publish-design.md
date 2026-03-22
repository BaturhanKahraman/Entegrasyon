# N11 Urun Publish — Sprint 3 Design Spec

## Problem

N11 pazaryerine urun publish etmek icin urun verilerini N11 SaveProduct SOAP formatina donusturecek bir mapper, eslestirme dogrulayici (validator) ve gercek SOAP cagrilarini yapacak bir service gerekiyor. Sprint 1'de olsuturulan MockN11ProductService gercek implementasyonla degistirilecek.

## Scope

- N11MappingValidator: publish oncesi eslestirme dogrulamasi
- N11ProductMapper: urun + varyant → N11 SaveProduct XML mapping
- N11ProductService (gercek): SaveProduct, DeleteProduct, UpdateProductBasic, StartSelling, StopSelling
- IN11ProductService interface genisletme (UpdateBasic, StartSelling, StopSelling)
- ProductMarketplace kaydini guncelleme (ExternalProductId, Status)
- Activity logging (Trendyol pattern'i)

## Out of Scope

- Stok/fiyat sync (Sprint 4 — ayri servis)
- Siparis/iade (gelecek faz)
- UI degisiklikleri (mevcut MarketplacePublishStep.razor zaten marketplace-agnostik)

---

## Architecture

### Existing Trendyol Pattern

```
TrendyolMappingValidator.ValidateProductMappingsAsync(productId)
    ↓
TrendyolProductMapper.MapProductAsync(productId) → TrendyolCreateProductRequest
    ↓
TrendyolApiClient.PostAsync(url, request) → BatchRequestId
    ↓
ProductMarketplace.BatchRequestId = batchRequestId
```

### N11 Pattern (Senkron, Batch Yok)

```
N11MappingValidator.ValidateProductMappingsAsync(productId)
    ↓
N11ProductMapper.MapProductAsync(productId) → XElement (SaveProduct XML body)
    ↓
N11SoapClient.SendAsync("ProductService", "", xmlBody) → Response with N11 ProductId
    ↓
ProductMarketplace.ExternalProductId = n11ProductId
ProductMarketplace.Status = Published
```

Temel fark: Trendyol batch + async polling kullanirken, N11 senkron SaveProduct kullanir. BatchRequestId yerine dogrudan N11 product ID doner.

---

## Components

### 1. N11MappingValidator

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11MappingValidator.cs`

**Dependencies:** `IDbContextFactory<IntegrationDbContext>`, `ILogger`

Pre-flight validation — publish oncesi calisir:

1. **Product kontrolu** — Urun var mi, CategoryId set mi?
2. **Kategori eslesmesi** — `CategoryMarketplace` tablosunda `MarketPlaceId=2` kaydi var mi?
3. **Brand kontrolu** — Product.BrandId null degilse, `BrandMarketPlaceMatch` tablosunda `MarketPlaceId=2` kaydi var mi?
4. **Zorunlu attribute kontrolu** — `CategoryAttributeCategory` tablosunda `IsRequired=true` olan attribute'larin hepsinin `CategoryAttributeMarketPlaceMatch` tablosunda `MarketPlaceId=2` eslesmesi var mi?

Returns: `IResult` (Success veya hata mesajlariyla Error)

### 2. N11ProductMapper

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs`

**Interface:** `IN11ProductMapper` (yeni interface)

**Dependencies:** `IDbContextFactory<IntegrationDbContext>`, `IMinioFileStorage`, `ILogger`

```csharp
public interface IN11ProductMapper
{
    Task<IDataResult<XElement>> MapProductAsync(Guid productId);
}
```

Donusu: `XElement` (SaveProductRequest icerisindeki `<product>` XML elementi)

**Mapping akisi:**

1. **Product fetch** — Product + Variants (with BranchOfficeStocks, Images) + AttributeKeyValues
2. **ProductMarketplace fetch** — TitleOverride, DescriptionOverride, VariantOverrides
3. **Marketplace matches fetch** — CategoryMarketplace, BrandMarketPlaceMatch, CategoryAttributeMarketPlaceMatch, CategoryAttributeValueMarketPlaceMatch (hepsi MarketPlaceId=2)
4. **Warehouse config** — MarketPlaceWarehouses (MarketPlaceId=2) veya fallback IsDefaultMarketPlaceStock
5. **XML building:**

```xml
<product>
    <productSellerCode>{product.ProductCode ?? product.Id}</productSellerCode>
    <title>{titleOverride ?? product.Title}</title>
    <description>{descriptionOverride ?? product.Description}</description>
    <category><id>{n11CategoryId}</id></category>
    <price>{firstVariant.ListPrice}</price>
    <currencyType>3</currencyType>
    <images>
        <!-- Product images, ordered, public URL from MinIO -->
        <image><url>{publicUrl}</url><order>{i+1}</order></image>
    </images>
    <stockItems>
        <!-- Per variant -->
        <stockItem>
            <sellerStockCode>{variant.Barcode}</sellerStockCode>
            <quantity>{sumOfWarehouseStocks}</quantity>
            <optionPrice>{salePriceOverride ?? variant.SalePrice}</optionPrice>
            <attributes>
                <!-- Variant-level attributes as name-value pairs -->
                <attribute>
                    <name>{attributeHumanizedName}</name>
                    <value>{attributeValueName}</value>
                </attribute>
            </attributes>
        </stockItem>
    </stockItems>
    <productCondition>1</productCondition>
    <preparingDay>3</preparingDay>
    <domestic>false</domestic>
</product>
```

**Override resolution:** Trendyol pattern'i ile ayni — `ProductMarketplace.TitleOverride ?? product.Title`, `ProductVariantMarketplaceOverride.SalePriceOverride ?? variant.SalePrice`.

**Image handling:** Product-level images (variant images degil), MinIO `GetPublicUrl(storageKey)` ile public URL'e cevrilir, `order` 1-based.

**Attribute mapping (name-value):**
- N11'de attribute'lar integer ID degil, string name-value pair olarak gider
- Product-level `AttributeKeyValues` icin: `CategoryAttribute.CategoryAttributeHumanized` → name, `CategoryAttributeValue.Name` → value (veya CustomValue)
- Variant-level attributes icin: varyant attribute'larindan name-value cekir

**Stok hesaplama:** MarketPlaceWarehouses (MarketPlaceId=2) ile belirlenen branch office'lerden toplam stok.

### 3. N11ProductService (Gercek Implementasyon)

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs`

**Implements:** `IN11ProductService`

**Dependencies:** `IN11SoapClient`, `IN11ProductMapper`, `N11MappingValidator`, `IProductActivityLogger`, `IDbContextFactory<IntegrationDbContext>`, `ILogger`

#### SaveProductAsync(Guid productId) → IDataResult<long>

1. Validate: `N11MappingValidator.ValidateProductMappingsAsync(productId)` → log activity
2. Map: `N11ProductMapper.MapProductAsync(productId)` → XElement
3. Build SOAP request: `SaveProductRequest` envelope with mapped product element
4. Send: `N11SoapClient.SendAsync("ProductService", "", request)`
5. Parse response: extract `<product><id>` → N11 product ID (long)
6. Update DB: `ProductMarketplace.ExternalProductId = n11ProductId.ToString()`, `Status = Published`
7. Log activity: `PublishSent`, Success

#### DeleteProductAsync(Guid productId) → IResult

1. Fetch ProductMarketplace (MarketPlaceId=2) → get ExternalProductId
2. Build SOAP: `DeleteProductByIdRequest` with `<productId>{externalId}</productId>`
3. Send via N11SoapClient
4. Update DB: `ProductMarketplace.Status = Pending` (or delete record)
5. Log activity

#### UpdateProductBasicAsync(Guid productId) → IResult

1. Map product (same mapper)
2. Build SOAP: `UpdateProductBasicRequest` with price, stockItems, description, images
3. Send via N11SoapClient
4. Log activity: ContentUpdated

#### StartSellingAsync(Guid productId) → IResult

1. Fetch ExternalProductId from ProductMarketplace
2. Build SOAP: `StartSellingProductByProductIdRequest` with `<productId>{id}</productId>`
3. Send via N11SoapClient → parse response
4. Log activity

#### StopSellingAsync(Guid productId) → IResult

1. Fetch ExternalProductId from ProductMarketplace
2. Build SOAP: `StopSellingProductByProductIdRequest` with `<productId>{id}</productId>`
3. Send via N11SoapClient
4. Log activity

### 4. IN11ProductService Interface Genisletme

**File:** `Application/Entegrasyon.Business/Abstract/IN11ProductService.cs` (modify)

```csharp
public interface IN11ProductService
{
    Task<IDataResult<long>> SaveProductAsync(Guid productId);
    Task<IResult> DeleteProductAsync(Guid productId);
    Task<IResult> UpdateProductBasicAsync(Guid productId);
    Task<IResult> StartSellingAsync(Guid productId);
    Task<IResult> StopSellingAsync(Guid productId);
}
```

MockN11ProductService'e de yeni metodlarin mock implementasyonu eklenmeli.

---

## DI Registration

```csharp
// AddApplicationDependencies:
services.AddScoped<N11MappingValidator>();
services.AddScoped<IN11ProductMapper, N11ProductMapper>();

// Mevcut N11:UseMock pattern'i henuz yok — dogrudan mock/real secimi:
// TODO: Sprint tamamlaninca N11:UseMock config ekle
// Simdilik gercek implementasyonu kaydet:
services.AddScoped<IN11ProductService, N11ProductService>();
// Eski mock'u kaldir
```

---

## Error Handling

- Validator basarisiz → ErrorResult + activity log (MappingValidated, Error)
- Mapper basarisiz (urun yok, varyant yok, image yok) → ErrorDataResult
- SOAP basarisiz → HttpRequestException, activity log (PublishSent, Error)
- N11 response `<status>failure</status>` → parse `<errorMessage>`, return ErrorResult
- ExternalProductId bulunamayan islemler (Delete/Start/Stop) → ErrorResult

## Testing

### Unit Tests (`Test/Entegrasyon.Test/N11/`)

**N11MappingValidatorTests.cs:**
1. ValidateAsync_WhenAllMappingsExist_ShouldReturnSuccess
2. ValidateAsync_WhenCategoryMatchMissing_ShouldReturnError
3. ValidateAsync_WhenBrandMatchMissing_ShouldReturnError
4. ValidateAsync_WhenRequiredAttributeMissing_ShouldReturnError

**N11ProductMapperTests.cs:**
1. MapProductAsync_ShouldReturnValidXml
2. MapProductAsync_ShouldMapVariantsAsStockItems
3. MapProductAsync_ShouldResolveOverrides
4. MapProductAsync_WhenProductNotFound_ShouldReturnError
5. MapProductAsync_ShouldMapAttributesAsNameValuePairs
6. MapProductAsync_ShouldOrderImages

**N11ProductServiceTests.cs:**
1. SaveProductAsync_ShouldCallValidatorMapperAndSoap
2. SaveProductAsync_ShouldUpdateProductMarketplace
3. DeleteProductAsync_ShouldCallSoapWithExternalId
4. StartSellingAsync_ShouldCallSoap
5. StopSellingAsync_ShouldCallSoap

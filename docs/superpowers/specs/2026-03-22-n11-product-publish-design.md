# N11 Urun Publish — Sprint 3 Design Spec

## Problem

N11 pazaryerine urun publish etmek icin urun verilerini N11 SaveProduct SOAP formatina donusturecek bir mapper, eslestirme dogrulayici (validator) ve gercek SOAP cagrilarini yapacak bir service gerekiyor. Sprint 1'de olusturulan MockN11ProductService gercek implementasyonla degistirilecek.

## Scope

- N11MappingValidator: publish oncesi eslestirme dogrulamasi
- N11ProductMapper: urun + varyant → N11 SaveProduct XML mapping
- N11ProductService (gercek): SaveProduct, DeleteProduct, UpdateProductBasic, StartSelling, StopSelling
- IN11ProductService interface genisletme (UpdateBasic, StartSelling, StopSelling)
- MockN11ProductService guncelleme (yeni metodlarin mock implementasyonu)
- ProductMarketplace kaydini guncelleme (ExternalProductId, Status)
- Activity logging (Trendyol pattern'i)

## Out of Scope

- Stok/fiyat sync (Sprint 4 — ayri servis)
- Siparis/iade (gelecek faz)
- UI degisiklikleri (mevcut MarketplacePublishStep.razor zaten marketplace-agnostik)

---

## Architecture

### N11 Pattern (Senkron, Batch Yok)

```
N11MappingValidator.ValidateProductMappingsAsync(productId)
    ↓
N11ProductMapper.MapProductAsync(productId) → XElement (SaveProduct XML body)
    ↓
N11SoapClient.SendAsync("ProductService", "", xmlBody) → Response XElement
    ↓
Check <result><status> — "failure" ise ErrorResult don
    ↓
Parse <product><id> → N11 product ID (long)
    ↓
ProductMarketplace.ExternalProductId = n11ProductId
ProductMarketplace.Status = Published
```

Temel fark: Trendyol batch + async polling kullanirken, N11 senkron SaveProduct kullanir. BatchRequestId yerine dogrudan N11 product ID doner.

---

## WSDL Endpoint Mapping

| Islem | WSDL Service (wsdlPath) | Request Element |
|-------|------------------------|-----------------|
| SaveProduct | `ProductService` | `SaveProductRequest` |
| DeleteProduct | `ProductService` | `DeleteProductByIdRequest` |
| UpdateProductBasic | `ProductService` | `UpdateProductBasicRequest` |
| StartSelling | `ProductSellingService` | `StartSellingProductByProductIdRequest` |
| StopSelling | `ProductSellingService` | `StopSellingProductByProductIdRequest` |

**Onemli:** StartSelling/StopSelling farkli bir WSDL service'e gider (`ProductSellingService`), `ProductService` degil.

---

## Components

### 1. N11MappingValidator

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11MappingValidator.cs`

**Dependencies:** `IDbContextFactory<IntegrationDbContext>`, `ILogger`

Pre-flight validation — publish oncesi calisir:

1. **Product kontrolu** — Urun var mi, CategoryId set mi?
2. **Kategori eslesmesi** — `CategoryMarketplace` tablosunda `MarketPlaceId=2` kaydi var mi?
3. **Brand kontrolu** — Product.BrandId null degilse, `BrandMarketPlaceMatch` tablosunda `MarketPlaceId=2` kaydi var mi? **Not:** N11'de brand zorunlu degil (Trendyol'dan farkli). BrandId null ise bu adim atlanir — brand N11'de name-value attribute olarak gider, zorunlu degilse hata degil.
4. **Zorunlu attribute kontrolu** — `CategoryAttributeCategory` tablosunda `IsRequired=true` olan attribute'larin hepsinin `CategoryAttributeMarketPlaceMatch` tablosunda `MarketPlaceId=2` eslesmesi var mi?

Returns: `IResult` (Success veya hata mesajlariyla Error)

### 2. N11ProductMapper

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11ProductMapper.cs`

**Interface:** `IN11ProductMapper` at `Application/Entegrasyon.Business/Abstract/IN11ProductMapper.cs`

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
    <title>{titleOverride ?? product.Title} (max 100 char)</title>
    <description>{descriptionOverride ?? product.Description}</description>
    <category><id>{n11CategoryId}</id></category>
    <price>{firstVariant.ListPrice}</price>
    <currencyType>1</currencyType>  <!-- 1=TL (N11 API dokumani) -->
    <!-- Product-level attributes (marka, malzeme vb.) -->
    <attributes>
        <attribute>
            <name>{productAttr.CategoryAttributeHumanized}</name>
            <value>{productAttr.ValueName ?? productAttr.CustomValue}</value>
        </attribute>
    </attributes>
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
            <!-- Variant-level attributes (Renk, Beden vb.) -->
            <attributes>
                <attribute>
                    <name>{variantAttr.HumanizedName}</name>
                    <value>{variantAttr.ValueName}</value>
                </attribute>
            </attributes>
        </stockItem>
    </stockItems>
    <productCondition>1</productCondition>
    <preparingDay>3</preparingDay>
    <domestic>false</domestic>
</product>
```

**Iki seviye attribute:**
- **Product-level `<attributes>`:** `AttributeKeyValues` uzerinden — urun ozellikleri (marka, malzeme, vb.). `product` elementinin dogrudan child'i.
- **Variant-level `<stockItem><attributes>`:** Varyant ozellikleri (Renk, Beden). Her stockItem'in icinde.

**Override resolution:** `ProductMarketplace.TitleOverride ?? product.Title`, `ProductVariantMarketplaceOverride.SalePriceOverride ?? variant.SalePrice`.

**Image handling:** Product-level images, MinIO `GetPublicUrl(storageKey)` ile public URL'e cevrilir, `order` 1-based.

**Stok hesaplama:** MarketPlaceWarehouses (MarketPlaceId=2) ile belirlenen branch office'lerden toplam stok.

### 3. N11ProductService (Gercek Implementasyon)

**File:** `Application/Entegrasyon.Business/Concrete/N11/N11ProductService.cs`

**Implements:** `IN11ProductService`

**Dependencies:** `IN11SoapClient`, `IN11ProductMapper`, `N11MappingValidator`, `IProductActivityLogger`, `IDbContextFactory<IntegrationDbContext>`, `ILogger`

#### SaveProductAsync(Guid productId) → IDataResult<long>

1. Validate: `N11MappingValidator.ValidateProductMappingsAsync(productId)` → log activity (MappingValidated)
2. Map: `N11ProductMapper.MapProductAsync(productId)` → XElement (`<product>`)
3. Build SOAP request: `SaveProductRequest` (ns=`http://www.n11.com/ws/schemas`) with mapped product element
4. Send: `N11SoapClient.SendAsync("ProductService", "", request)`
5. **Response status check:** Parse `response.Element("result")?.Element("status")?.Value` — eger "failure" ise `response.Element("result")?.Element("errorMessage")?.Value` al, ErrorResult don
6. Parse product ID: `response.Element("product")?.Element("id")?.Value` → long.Parse
7. Update DB: `ProductMarketplace.ExternalProductId = n11ProductId.ToString()`, `Status = Published`
8. Log activity: `PublishSent`, Success, referenceId = n11ProductId

#### DeleteProductAsync(Guid productId) → IResult

1. Fetch ProductMarketplace (MarketPlaceId=2) → get ExternalProductId. Yoksa ErrorResult.
2. Build SOAP: `DeleteProductByIdRequest` (ns=schemas) with `<productId>{externalId}</productId>`
3. Send: `N11SoapClient.SendAsync("ProductService", "", request)`
4. Check response status (same pattern)
5. Update DB: `ProductMarketplace.Status = Pending`
6. Log activity: Deleted

#### UpdateProductBasicAsync(Guid productId) → IResult

**Not:** UpdateProductBasic tam mapper KULLANMAZ. Hafif bir mapping yapar — sadece sinirlı alanlar gonderilir.

1. Fetch Product + Variants + ProductMarketplace (MarketPlaceId=2). ExternalProductId yoksa ErrorResult.
2. Build `UpdateProductBasicRequest` XML:
   ```xml
   <productId>{externalProductId}</productId>
   <price>{listPrice}</price>
   <description>{descriptionOverride ?? description}</description>
   <stockItems>
       <stockItem>
           <sellerStockCode>{variant.Barcode}</sellerStockCode>
           <quantity>{stock}</quantity>
           <optionPrice>{salePrice}</optionPrice>
       </stockItem>
   </stockItems>
   <images>
       <image><url>{publicUrl}</url><order>{i+1}</order></image>
   </images>
   ```
3. Send: `N11SoapClient.SendAsync("ProductService", "", request)`
4. Check response status
5. Log activity: ContentUpdated

#### StartSellingAsync(Guid productId) → IResult

1. Fetch ExternalProductId from ProductMarketplace. Yoksa ErrorResult.
2. Build SOAP: `StartSellingProductByProductIdRequest` (ns=schemas) with `<productId>{id}</productId>`
3. Send: `N11SoapClient.SendAsync("ProductSellingService", "", request)` ← **FARKLI WSDL**
4. Check response status
5. Log activity

#### StopSellingAsync(Guid productId) → IResult

1. Fetch ExternalProductId from ProductMarketplace. Yoksa ErrorResult.
2. Build SOAP: `StopSellingProductByProductIdRequest` (ns=schemas) with `<productId>{id}</productId>`
3. Send: `N11SoapClient.SendAsync("ProductSellingService", "", request)` ← **FARKLI WSDL**
4. Check response status
5. Log activity

### 4. IN11ProductService Interface Genisletme + Mock Guncelleme

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

**File:** `Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs` (modify)

Yeni 3 metod icin mock implementasyon eklenmeli (SuccessResult donen basit loglamalar). Bu build'i kirmamasi icin interface genisletmesiyle ayni task'ta yapilmali.

---

## Response Status Check Pattern

Tum N11 SOAP response'lari `<result><status>success|failure</status></result>` iceriri. `N11SoapClient` sadece HTTP status kontrol eder — SOAP-level status kontrolu `N11ProductService` icerisinde yapilir:

```csharp
private static IResult CheckN11ResponseStatus(XElement response)
{
    var status = response.Element("result")?.Element("status")?.Value;
    if (status == "failure")
    {
        var errorMessage = response.Element("result")?.Element("errorMessage")?.Value
            ?? "Bilinmeyen N11 hatasi";
        return new ErrorResult(errorMessage);
    }
    return new SuccessResult();
}
```

Bu helper tum SOAP islemlerinde (Save/Delete/Update/Start/Stop) ortaktir.

---

## DI Registration

```csharp
// AddApplicationDependencies:
services.AddScoped<N11MappingValidator>();
services.AddScoped<IN11ProductMapper, N11ProductMapper>();

// Mevcut mock kaydi degistir:
// REMOVE: services.AddScoped<IN11ProductService, MockN11ProductService>();
// ADD:
var useN11Mock = configuration.GetValue<bool>("N11:UseMock", true);
if (useN11Mock)
{
    services.AddScoped<IN11ProductService, MockN11ProductService>();
}
else
{
    services.AddScoped<IN11ProductService, N11ProductService>();
}
```

---

## Error Handling

- Validator basarisiz → ErrorResult + activity log (MappingValidated, Error)
- Mapper basarisiz (urun yok, varyant yok, image yok) → ErrorDataResult
- SOAP HTTP basarisiz → HttpRequestException (N11SoapClient firlatir), activity log (PublishSent, Error)
- N11 response `<result><status>failure</status>` → parse `<errorMessage>`, return ErrorResult
- ExternalProductId bulunamayan islemler (Delete/Update/Start/Stop) → ErrorResult
- Brand null → validator gecerli sayar (N11'de brand zorunlu degil), mapper brand attribute'unu atlar

## Testing

### Unit Tests (`Test/Entegrasyon.Test/N11/`)

**N11MappingValidatorTests.cs:**
1. ValidateAsync_WhenAllMappingsExist_ShouldReturnSuccess
2. ValidateAsync_WhenCategoryMatchMissing_ShouldReturnError
3. ValidateAsync_WhenBrandMatchMissing_ShouldReturnError
4. ValidateAsync_WhenRequiredAttributeMissing_ShouldReturnError
5. ValidateAsync_WhenBrandIdNull_ShouldReturnSuccess (N11-specific)

**N11ProductMapperTests.cs:**
1. MapProductAsync_ShouldReturnValidXml
2. MapProductAsync_ShouldMapVariantsAsStockItems
3. MapProductAsync_ShouldResolveOverrides
4. MapProductAsync_WhenProductNotFound_ShouldReturnError
5. MapProductAsync_ShouldMapProductLevelAttributes
6. MapProductAsync_ShouldMapVariantLevelAttributes
7. MapProductAsync_ShouldOrderImages
8. MapProductAsync_CurrencyType_ShouldBe1

**N11ProductServiceTests.cs:**
1. SaveProductAsync_ShouldCallValidatorMapperAndSoap
2. SaveProductAsync_ShouldUpdateProductMarketplace
3. SaveProductAsync_WhenResponseFailure_ShouldReturnError
4. DeleteProductAsync_ShouldCallSoapWithExternalId
5. UpdateProductBasicAsync_ShouldCallSoapWithLimitedFields
6. StartSellingAsync_ShouldUseSoapProductSellingService
7. StopSellingAsync_ShouldUseSoapProductSellingService

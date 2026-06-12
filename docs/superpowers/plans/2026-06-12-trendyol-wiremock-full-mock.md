# Trendyol WireMock Tam Mock İmplementasyon Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trendyol entegrasyonunun tüm endpoint'lerini WireMock ile doğru mocklamak; DevWireMockSeeder'a SellerId seed eklemek; batch + order lifecycle scenario'larını kurmak.

**Architecture:** `docs/wiremock/` altındaki mapping + body dosyaları git'te tutulur. `develop` push → Gitea CI `docs/wiremock/` içeriğini `/opt/stacks/entegrasyon-dev/wiremock/`'a kopyalar; WireMock container `:ro` mount ile dosyaları okur. Seeder startup'ta DB'deki `marketplace.SellerId`'yi `DevMode:TrendyolSellerId` config'inden okuyarak set eder.

**Tech Stack:** WireMock 3.9.2, ASP.NET Core 10, C#, JSON, Gitea Actions, SSH

---

## Dosya Haritası

**Modify:**
- `Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs`
- `Application/Entegrasyon.MVC/appsettings.Development.json` ← ASLA commit etme

**Create (test):**
- `Test/Entegrasyon.IntegrationTest/DevMode/DevWireMockSeederTests.cs`

**Delete (eski flat Trendyol dosyaları):**
- `docs/wiremock/mappings/trendyol/get-brands.json`
- `docs/wiremock/mappings/trendyol/get-orders.json`
- `docs/wiremock/mappings/trendyol/post-invoice-link.json`
- `docs/wiremock/mappings/trendyol/post-products.json`
- `docs/wiremock/mappings/trendyol/post-stock-price.json`
- `docs/wiremock/__files/trendyol/brand-search.json`
- `docs/wiremock/__files/trendyol/invoice-link.json`
- `docs/wiremock/__files/trendyol/orders-page1.json`
- `docs/wiremock/__files/trendyol/product-create.json`
- `docs/wiremock/__files/trendyol/stock-price-update.json`

**Create (mappings — `docs/wiremock/mappings/trendyol/`):**
- `catalog/brands-by-name.json`, `catalog/category-attributes.json`, `catalog/brands-page-first.json`, `catalog/brands-page-empty.json`
- `seller/addresses.json`
- `stock/update.json`, `stock/_400-invalid.json`
- `product/create.json`, `product/batch-inprogress.json`, `product/batch-completed.json`
- `product/update-unapproved.json`, `product/update-content.json`, `product/delete.json`, `product/_400-missing-items.json`
- `order/get-orders-created.json`, `order/get-orders-invoiced.json`, `order/get-orders-shipped.json`
- `order/update-unsupplied.json`, `order/update-status.json`, `order/get-shipping-label.json`
- `invoice/create-link.json`, `invoice/delete-link.json`, `invoice/upload-pdf.json`
- `_global-429.json`

**Create (body files — `docs/wiremock/__files/trendyol/`):**
- `catalog/brands-by-name.json`, `catalog/category-attributes.json`, `catalog/brands-page-first.json`, `catalog/brands-page-empty.json`
- `seller/addresses.json`
- `stock/update-response.json`
- `product/batch-response.json`, `product/batch-inprogress.json`, `product/batch-completed.json`
- `order/orders-created.json`, `order/orders-invoiced.json`, `order/orders-shipped.json`
- `invoice/link-response.json`

---

## Task 1: DevWireMockSeeder — SellerId Seeding

**Files:**
- Modify: `Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs`
- Create: `Test/Entegrasyon.IntegrationTest/DevMode/DevWireMockSeederTests.cs`
- Modify (local, no commit): `Application/Entegrasyon.MVC/appsettings.Development.json`

- [ ] **Step 1: Integration test yaz (RED)**

```csharp
// Test/Entegrasyon.IntegrationTest/DevMode/DevWireMockSeederTests.cs
using Entegrasyon.IntegrationTest.Fixtures;
using Entegrasyon.MVC.Infrastructure.DevMode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Entegrasyon.IntegrationTest.DevMode;

[Trait("Category", "Integration")]
public class DevWireMockSeederTests : IntegrationTestBase
{
    public DevWireMockSeederTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task SeedAsync_WhenTrendyolSellerIdConfigured_SetsSellerId()
    {
        // Arrange
        await SeedMarketPlaceAsync(1, "Trendyol");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevMode:WireMockUrl"] = "http://wiremock-test:8080",
                ["DevMode:TrendyolSellerId"] = "999111"
            })
            .Build();

        // Act
        await DevWireMockSeeder.SeedAsync(Services, config, NullLogger.Instance);

        // Assert
        using var db = CreateDbContext();
        var trendyol = await db.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == "Trendyol");

        trendyol.Should().NotBeNull();
        trendyol!.SellerId.Should().Be("999111");
        trendyol.BaseUrl.Should().Be("http://wiremock-test:8080");
    }

    [Fact]
    public async Task SeedAsync_WhenTrendyolSellerIdNotConfigured_DoesNotChangeSellerId()
    {
        // Arrange — WireMockUrl set ama TrendyolSellerId eksik → mevcut SellerId korunmalı
        await SeedMarketPlaceAsync(1, "Trendyol");

        using (var db = CreateDbContext())
        {
            var mp = await db.MarketPlaces.AsTracking().FirstAsync(m => m.Name == "Trendyol");
            mp.SellerId = "existing-seller";
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevMode:WireMockUrl"] = "http://wiremock-test:8080"
                // TrendyolSellerId yok
            })
            .Build();

        // Act
        await DevWireMockSeeder.SeedAsync(Services, config, NullLogger.Instance);

        // Assert — SellerId değişmemeli
        using var dbCheck = CreateDbContext();
        var trendyol = await dbCheck.MarketPlaces.AsNoTracking().FirstAsync(m => m.Name == "Trendyol");
        trendyol.SellerId.Should().Be("existing-seller");
    }
}
```

- [ ] **Step 2: Test çalıştır, RED doğrula**

```bash
DOCKER_HOST=unix:///tmp/docker-server.sock \
TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 \
TESTCONTAINERS_RYUK_DISABLED=true \
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~DevWireMockSeederTests"
```

Beklenen: 1-2 hata (SellerId set edilmiyor).

- [ ] **Step 3: DevWireMockSeeder.cs güncelle (GREEN)**

`SeedAsync` metoduna `testSellerId` okuma + Trendyol SellerId güncelleme ekle:

```csharp
// Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs
// Mevcut: var realApiMarketplaces = ...
// Altına ekle:
var testSellerId = configuration["DevMode:TrendyolSellerId"];

// Mevcut foreach içinde, BaseUrl if bloğundan SONRA:
if (!string.IsNullOrEmpty(testSellerId) &&
    string.Equals(mp.Name, "Trendyol", StringComparison.OrdinalIgnoreCase) &&
    mp.SellerId != testSellerId)
{
    mp.SellerId = testSellerId;
    updated++;
    logger.LogInformation("DevWireMockSeeder: Trendyol SellerId → {SellerId}", testSellerId);
}
```

Tam güncellenen `SeedAsync` metodu (tüm dosya değil, sadece değişen blok):

```csharp
public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
{
    var wireMockUrl = configuration["DevMode:WireMockUrl"];
    if (string.IsNullOrWhiteSpace(wireMockUrl))
    {
        logger.LogDebug("DevWireMockSeeder: DevMode:WireMockUrl tanimli degil, atlaniyor.");
        return;
    }

    var realApiMarketplaces = configuration.GetSection("DevMode:RealApiMarketplaces").Get<string[]>() ?? [];
    var testSellerId = configuration["DevMode:TrendyolSellerId"];

    try
    {
        using var scope = serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = contextFactory.CreateDbContext();

        var marketplaces = await db.MarketPlaces.AsTracking().ToListAsync();
        var updated = 0;

        foreach (var mp in marketplaces)
        {
            if (realApiMarketplaces.Contains(mp.Name, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogDebug(
                    "DevWireMockSeeder: {Marketplace} gercek API listesinde — BaseUrl korunuyor ({BaseUrl})",
                    mp.Name, mp.BaseUrl);
                continue;
            }

            if (mp.BaseUrl != wireMockUrl)
            {
                var previous = mp.BaseUrl;
                mp.BaseUrl = wireMockUrl;
                updated++;
                logger.LogInformation(
                    "DevWireMockSeeder: {Marketplace} BaseUrl {Previous} → {New}",
                    mp.Name, previous, wireMockUrl);
            }

            if (!string.IsNullOrEmpty(testSellerId) &&
                string.Equals(mp.Name, "Trendyol", StringComparison.OrdinalIgnoreCase) &&
                mp.SellerId != testSellerId)
            {
                mp.SellerId = testSellerId;
                updated++;
                logger.LogInformation("DevWireMockSeeder: Trendyol SellerId → {SellerId}", testSellerId);
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("DevWireMockSeeder: {Count} marketplace guncellendi.", updated);
        }
        else
        {
            logger.LogDebug("DevWireMockSeeder: Degisiklik yok.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "DevWireMockSeeder: DB update basarisiz, gecillenecek. Hata: {Message}", ex.Message);
    }
}
```

- [ ] **Step 4: Test tekrar çalıştır, GREEN doğrula**

```bash
DOCKER_HOST=unix:///tmp/docker-server.sock \
TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 \
TESTCONTAINERS_RYUK_DISABLED=true \
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj \
  --filter "FullyQualifiedName~DevWireMockSeederTests"
```

Beklenen: 2/2 passed.

- [ ] **Step 5: appsettings.Development.json güncelle (COMMIT ETME)**

`DevMode` bölümüne `TrendyolSellerId` ekle:

```json
"DevMode": {
  "//": "WireMock container varsa marketplace BaseUrl'lerini buraya yonlendirir. Bos birakilirsa seeder pas gecer.",
  "WireMockUrl": "",
  "TrendyolSellerId": "123456",
  "//RealApiMarketplaces": "Gercek sandbox API'ye gitmesi istenen marketplace adlari. Seeder bunlara dokunmaz. Ornek: [\"Trendyol\"]",
  "RealApiMarketplaces": []
}
```

**DİKKAT:** Bu dosya staged OLMAMALI. Sadece working-tree'de kalır.

- [ ] **Step 6: Commit (appsettings hariç)**

```bash
git add Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs
git add Test/Entegrasyon.IntegrationTest/DevMode/DevWireMockSeederTests.cs
git commit -m "feat(dev): DevWireMockSeeder Trendyol SellerId seeding

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2: compose.yaml Server Güncellemesi

**Files:**
- Modify (server): `/opt/stacks/entegrasyon-dev/compose.yaml`

- [ ] **Step 1: compose.yaml'a DevMode__TrendyolSellerId ekle**

```bash
ssh server "python3 -c \"
content = open('/opt/stacks/entegrasyon-dev/compose.yaml').read()
if 'TrendyolSellerId' not in content:
    content = content.replace(
        '- DevMode__WireMockUrl=http://wiremock:8080',
        '- DevMode__WireMockUrl=http://wiremock:8080\n      - DevMode__TrendyolSellerId=123456'
    )
    open('/opt/stacks/entegrasyon-dev/compose.yaml', 'w').write(content)
    print('Updated')
else:
    print('Already set')
\""
```

- [ ] **Step 2: Değişikliği doğrula**

```bash
ssh server "grep 'TrendyolSellerId' /opt/stacks/entegrasyon-dev/compose.yaml"
```

Beklenen: `- DevMode__TrendyolSellerId=123456`

---

## Task 3: Eski Flat Dosyaları Sil + Catalog Mappings

**Files:**
- Delete: `docs/wiremock/mappings/trendyol/{get-brands,get-orders,post-invoice-link,post-products,post-stock-price}.json`
- Delete: `docs/wiremock/__files/trendyol/{brand-search,invoice-link,orders-page1,product-create,stock-price-update}.json`
- Create: `docs/wiremock/mappings/trendyol/catalog/` + `docs/wiremock/__files/trendyol/catalog/`

- [ ] **Step 1: Eski dosyaları sil**

```bash
git rm docs/wiremock/mappings/trendyol/get-brands.json \
       docs/wiremock/mappings/trendyol/get-orders.json \
       docs/wiremock/mappings/trendyol/post-invoice-link.json \
       docs/wiremock/mappings/trendyol/post-products.json \
       docs/wiremock/mappings/trendyol/post-stock-price.json \
       docs/wiremock/__files/trendyol/brand-search.json \
       docs/wiremock/__files/trendyol/invoice-link.json \
       docs/wiremock/__files/trendyol/orders-page1.json \
       docs/wiremock/__files/trendyol/product-create.json \
       docs/wiremock/__files/trendyol/stock-price-update.json
```

- [ ] **Step 2: docs/wiremock/mappings/trendyol/catalog/brands-by-name.json**

```json
{
  "priority": 5,
  "request": {
    "method": "GET",
    "urlPathPattern": "/product/brands/by-name"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/catalog/brands-by-name.json"
  }
}
```

- [ ] **Step 3: docs/wiremock/__files/trendyol/catalog/brands-by-name.json**

```json
{
  "brands": [
    { "id": 1001, "name": "Mock Brand A" },
    { "id": 1002, "name": "Mock Brand B" },
    { "id": 1003, "name": "Test Brand" }
  ]
}
```

- [ ] **Step 4: docs/wiremock/mappings/trendyol/catalog/category-attributes.json**

```json
{
  "priority": 5,
  "request": {
    "method": "GET",
    "urlPathPattern": "/product/product-categories/[0-9]+/attributes$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/catalog/category-attributes.json"
  }
}
```

- [ ] **Step 5: docs/wiremock/__files/trendyol/catalog/category-attributes.json**

```json
{
  "id": 388,
  "name": "Çocuk Giyim",
  "displayName": "Çocuk Giyim",
  "categoryAttributes": [
    {
      "allowCustom": false,
      "required": true,
      "varianter": false,
      "slicer": true,
      "categoryId": 388,
      "attribute": { "id": 348, "name": "Renk" },
      "attributeValues": [
        { "id": 1001, "name": "Mavi" },
        { "id": 1002, "name": "Kırmızı" },
        { "id": 1003, "name": "Beyaz" }
      ]
    },
    {
      "allowCustom": false,
      "required": true,
      "varianter": true,
      "slicer": false,
      "categoryId": 388,
      "attribute": { "id": 338, "name": "Beden" },
      "attributeValues": [
        { "id": 2001, "name": "2-3 Yaş" },
        { "id": 2002, "name": "4-5 Yaş" },
        { "id": 2003, "name": "6-7 Yaş" }
      ]
    },
    {
      "allowCustom": true,
      "required": false,
      "varianter": false,
      "slicer": false,
      "categoryId": 388,
      "attribute": { "id": 122, "name": "Cinsiyet" },
      "attributeValues": [
        { "id": 3001, "name": "Erkek" },
        { "id": 3002, "name": "Kız" },
        { "id": 3003, "name": "Unisex" }
      ]
    }
  ]
}
```

- [ ] **Step 6: docs/wiremock/mappings/trendyol/catalog/brands-page-first.json**

Marka importer page=-1 çağrısı — markaları döner, loop devam eder:

```json
{
  "priority": 3,
  "request": {
    "method": "GET",
    "urlPath": "/product/brands",
    "queryParameters": {
      "page": { "equalTo": "-1" }
    }
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/catalog/brands-page-first.json"
  }
}
```

- [ ] **Step 7: docs/wiremock/__files/trendyol/catalog/brands-page-first.json**

```json
{
  "brands": [
    { "id": 7651, "name": "Nike" },
    { "id": 7652, "name": "Adidas" },
    { "id": 7653, "name": "Zara" },
    { "id": 7654, "name": "H&M" },
    { "id": 7655, "name": "LC Waikiki" }
  ]
}
```

- [ ] **Step 8: docs/wiremock/mappings/trendyol/catalog/brands-page-empty.json**

Diğer tüm page değerleri — boş döner, loop durur:

```json
{
  "priority": 5,
  "request": {
    "method": "GET",
    "urlPathPattern": "/product/brands"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/catalog/brands-page-empty.json"
  }
}
```

- [ ] **Step 9: docs/wiremock/__files/trendyol/catalog/brands-page-empty.json**

```json
{ "brands": [] }
```

- [ ] **Step 10: Commit**

```bash
git add docs/wiremock/mappings/trendyol/catalog/
git add docs/wiremock/__files/trendyol/catalog/
git commit -m "feat(wiremock/trendyol): eski flat dosyalar silindi, catalog mappings eklendi

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4: Seller + Stock Mappings

**Files:**
- Create: `docs/wiremock/mappings/trendyol/seller/addresses.json`
- Create: `docs/wiremock/__files/trendyol/seller/addresses.json`
- Create: `docs/wiremock/mappings/trendyol/stock/update.json`
- Create: `docs/wiremock/mappings/trendyol/stock/_400-invalid.json`
- Create: `docs/wiremock/__files/trendyol/stock/update-response.json`

- [ ] **Step 1: docs/wiremock/mappings/trendyol/seller/addresses.json**

```json
{
  "priority": 5,
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/sellers/[0-9]+/addresses$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/seller/addresses.json"
  }
}
```

- [ ] **Step 2: docs/wiremock/__files/trendyol/seller/addresses.json**

```json
{
  "supplierAddresses": [
    {
      "id": 1,
      "fullAddress": "Test Depo Mah. Sanayi Sk. No:5 Daire:1",
      "city": "İstanbul",
      "district": "Pendik",
      "postalCode": "34890",
      "isDefault": true,
      "isShipmentAddress": true,
      "isInvoiceAddress": true,
      "isReturningAddress": false
    }
  ]
}
```

- [ ] **Step 3: docs/wiremock/mappings/trendyol/stock/_400-invalid.json**

Validation error — priority 3 (yüksek, happy-path'ten önce eşleşir):

```json
{
  "priority": 3,
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/inventory/sellers/[0-9]+/products/price-and-inventory$",
    "bodyPatterns": [
      { "matchesJsonPath": "$.items[0]", "absent": true }
    ]
  },
  "response": {
    "status": 400,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {
      "errors": [
        { "code": "VALIDATION_ERROR", "message": "items cannot be empty" }
      ]
    }
  }
}
```

- [ ] **Step 4: docs/wiremock/mappings/trendyol/stock/update.json**

```json
{
  "priority": 5,
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/inventory/sellers/[0-9]+/products/price-and-inventory$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/stock/update-response.json"
  }
}
```

- [ ] **Step 5: docs/wiremock/__files/trendyol/stock/update-response.json**

```json
{ "batchRequestId": "mock-stock-price-batch-67890" }
```

- [ ] **Step 6: Commit**

```bash
git add docs/wiremock/mappings/trendyol/seller/
git add docs/wiremock/__files/trendyol/seller/
git add docs/wiremock/mappings/trendyol/stock/
git add docs/wiremock/__files/trendyol/stock/
git commit -m "feat(wiremock/trendyol): seller + stock mappings

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5: Product Mappings + batch-lifecycle Scenario

**Files:**
- Create: `docs/wiremock/mappings/trendyol/product/*.json` (7 dosya)
- Create: `docs/wiremock/__files/trendyol/product/*.json` (3 dosya)

- [ ] **Step 1: docs/wiremock/__files/trendyol/product/batch-response.json**

```json
{ "batchRequestId": "batch-test-001" }
```

- [ ] **Step 2: docs/wiremock/__files/trendyol/product/batch-inprogress.json**

```json
{
  "batchRequestId": "batch-test-001",
  "status": "IN_PROGRESS",
  "items": [],
  "itemCount": 1,
  "failedItemCount": 0,
  "batchRequestType": "ITEM",
  "creationDate": 1749729600000,
  "lastModification": 1749729601000
}
```

- [ ] **Step 3: docs/wiremock/__files/trendyol/product/batch-completed.json**

```json
{
  "batchRequestId": "batch-test-001",
  "status": "COMPLETED",
  "items": [
    { "requestItem": null, "status": "SUCCESS", "failureReasons": [] }
  ],
  "itemCount": 1,
  "failedItemCount": 0,
  "batchRequestType": "ITEM",
  "creationDate": 1749729600000,
  "lastModification": 1749729660000
}
```

- [ ] **Step 4: docs/wiremock/mappings/trendyol/product/_400-missing-items.json**

```json
{
  "priority": 3,
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/v2/products$",
    "bodyPatterns": [
      { "matchesJsonPath": "$.items[0]", "absent": true }
    ]
  },
  "response": {
    "status": 400,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {
      "errors": [
        { "code": "VALIDATION_ERROR", "message": "items is required and cannot be empty" }
      ]
    }
  }
}
```

- [ ] **Step 5: docs/wiremock/mappings/trendyol/product/create.json**

```json
{
  "priority": 5,
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/v2/products$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-response.json"
  }
}
```

- [ ] **Step 6: docs/wiremock/mappings/trendyol/product/batch-inprogress.json**

Scenario ilk state (Started) → IN_PROGRESS döner, batch-polled'a geçer:

```json
{
  "priority": 5,
  "scenarioName": "batch-lifecycle",
  "requiredScenarioState": "Started",
  "newScenarioState": "batch-polled",
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/products/batch-requests/[^/]+$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-inprogress.json"
  }
}
```

- [ ] **Step 7: docs/wiremock/mappings/trendyol/product/batch-completed.json**

Scenario ikinci state (batch-polled) → COMPLETED döner, Started'a sıfırlar:

```json
{
  "priority": 5,
  "scenarioName": "batch-lifecycle",
  "requiredScenarioState": "batch-polled",
  "newScenarioState": "Started",
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/products/batch-requests/[^/]+$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-completed.json"
  }
}
```

- [ ] **Step 8: docs/wiremock/mappings/trendyol/product/update-unapproved.json**

```json
{
  "priority": 5,
  "request": {
    "method": "PUT",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/v2/products/unapproved-bulk-update$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-response.json"
  }
}
```

- [ ] **Step 9: docs/wiremock/mappings/trendyol/product/update-content.json**

```json
{
  "priority": 5,
  "request": {
    "method": "PUT",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/v2/products/content-bulk-update$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-response.json"
  }
}
```

- [ ] **Step 10: docs/wiremock/mappings/trendyol/product/delete.json**

```json
{
  "priority": 5,
  "request": {
    "method": "DELETE",
    "urlPathPattern": "/integration/product/sellers/[0-9]+/products$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/product/batch-response.json"
  }
}
```

- [ ] **Step 11: Commit**

```bash
git add docs/wiremock/mappings/trendyol/product/
git add docs/wiremock/__files/trendyol/product/
git commit -m "feat(wiremock/trendyol): product mappings + batch-lifecycle scenario

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6: Order + Invoice Mappings + order-lifecycle Scenario

**Files:**
- Create: `docs/wiremock/mappings/trendyol/order/*.json` (6 dosya)
- Create: `docs/wiremock/mappings/trendyol/invoice/*.json` (3 dosya)
- Create: `docs/wiremock/__files/trendyol/order/*.json` (3 dosya)
- Create: `docs/wiremock/__files/trendyol/invoice/link-response.json`

- [ ] **Step 1: docs/wiremock/__files/trendyol/order/orders-created.json**

```json
{
  "page": 0,
  "size": 50,
  "totalPages": 1,
  "totalElements": 1,
  "content": [
    {
      "shipmentPackageId": 99001,
      "orderNumber": "TY-TEST-001",
      "orderDate": "2026-06-12T10:00:00+03:00",
      "status": "Created",
      "grossAmount": 299.90,
      "totalDiscount": 0.00,
      "totalPrice": 299.90,
      "micro": false,
      "fastDelivery": false,
      "estimatedDeliveryEndDate": "2026-06-15T23:59:59+03:00",
      "cargoProviderInfo": {
        "cargoProviderName": "Yurtiçi Kargo",
        "cargoTrackingNumber": null,
        "cargoTrackingLink": null
      },
      "customerInfo": {
        "firstName": "Test",
        "lastName": "Müşteri",
        "email": "test@example.com"
      },
      "shipmentAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "invoiceAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "lines": [
        {
          "lineId": 1001,
          "quantity": 1,
          "price": 299.90,
          "discount": 0.00,
          "barcode": "TEST-BARCODE-001",
          "merchantSku": "TEST-SKU-001",
          "productName": "Test Ürün",
          "productColor": "Mavi",
          "productSize": "M",
          "merchantId": 123456,
          "vatRate": 10
        }
      ]
    }
  ]
}
```

- [ ] **Step 2: docs/wiremock/__files/trendyol/order/orders-invoiced.json**

Aynı yapı, status → "Picking":

```json
{
  "page": 0,
  "size": 50,
  "totalPages": 1,
  "totalElements": 1,
  "content": [
    {
      "shipmentPackageId": 99001,
      "orderNumber": "TY-TEST-001",
      "orderDate": "2026-06-12T10:00:00+03:00",
      "status": "Picking",
      "grossAmount": 299.90,
      "totalDiscount": 0.00,
      "totalPrice": 299.90,
      "micro": false,
      "fastDelivery": false,
      "estimatedDeliveryEndDate": "2026-06-15T23:59:59+03:00",
      "cargoProviderInfo": {
        "cargoProviderName": "Yurtiçi Kargo",
        "cargoTrackingNumber": null,
        "cargoTrackingLink": null
      },
      "customerInfo": {
        "firstName": "Test",
        "lastName": "Müşteri",
        "email": "test@example.com"
      },
      "shipmentAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "invoiceAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "lines": [
        {
          "lineId": 1001,
          "quantity": 1,
          "price": 299.90,
          "discount": 0.00,
          "barcode": "TEST-BARCODE-001",
          "merchantSku": "TEST-SKU-001",
          "productName": "Test Ürün",
          "productColor": "Mavi",
          "productSize": "M",
          "merchantId": 123456,
          "vatRate": 10
        }
      ]
    }
  ]
}
```

- [ ] **Step 3: docs/wiremock/__files/trendyol/order/orders-shipped.json**

status → "Shipped", cargoTrackingNumber dolu:

```json
{
  "page": 0,
  "size": 50,
  "totalPages": 1,
  "totalElements": 1,
  "content": [
    {
      "shipmentPackageId": 99001,
      "orderNumber": "TY-TEST-001",
      "orderDate": "2026-06-12T10:00:00+03:00",
      "status": "Shipped",
      "grossAmount": 299.90,
      "totalDiscount": 0.00,
      "totalPrice": 299.90,
      "micro": false,
      "fastDelivery": false,
      "estimatedDeliveryEndDate": "2026-06-15T23:59:59+03:00",
      "cargoProviderInfo": {
        "cargoProviderName": "Yurtiçi Kargo",
        "cargoTrackingNumber": "TY-TRACK-001",
        "cargoTrackingLink": "https://www.yurticikargo.com/tr/online-islemler/gonderi-sorgula?code=TY-TRACK-001"
      },
      "customerInfo": {
        "firstName": "Test",
        "lastName": "Müşteri",
        "email": "test@example.com"
      },
      "shipmentAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "invoiceAddress": {
        "city": "İstanbul",
        "district": "Kadıköy",
        "fullAddress": "Test Mah. Test Sk. No:1",
        "postalCode": "34710",
        "countryCode": "TR"
      },
      "lines": [
        {
          "lineId": 1001,
          "quantity": 1,
          "price": 299.90,
          "discount": 0.00,
          "barcode": "TEST-BARCODE-001",
          "merchantSku": "TEST-SKU-001",
          "productName": "Test Ürün",
          "productColor": "Mavi",
          "productSize": "M",
          "merchantId": 123456,
          "vatRate": 10
        }
      ]
    }
  ]
}
```

- [ ] **Step 4: docs/wiremock/mappings/trendyol/order/get-orders-created.json**

Scenario başlangıç state (Started):

```json
{
  "priority": 5,
  "scenarioName": "order-lifecycle",
  "requiredScenarioState": "Started",
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/orders"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/order/orders-created.json"
  }
}
```

- [ ] **Step 5: docs/wiremock/mappings/trendyol/order/get-orders-invoiced.json**

```json
{
  "priority": 5,
  "scenarioName": "order-lifecycle",
  "requiredScenarioState": "order-invoiced",
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/orders"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/order/orders-invoiced.json"
  }
}
```

- [ ] **Step 6: docs/wiremock/mappings/trendyol/order/get-orders-shipped.json**

```json
{
  "priority": 5,
  "scenarioName": "order-lifecycle",
  "requiredScenarioState": "order-shipped",
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/orders"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/order/orders-shipped.json"
  }
}
```

- [ ] **Step 7: docs/wiremock/mappings/trendyol/order/update-unsupplied.json**

```json
{
  "priority": 5,
  "request": {
    "method": "PUT",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/unsupplied$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {}
  }
}
```

- [ ] **Step 8: docs/wiremock/mappings/trendyol/order/update-status.json**

Scenario: `order-invoiced` → `order-shipped`. `$` ile URL sonunu anchore et:

```json
{
  "priority": 5,
  "scenarioName": "order-lifecycle",
  "requiredScenarioState": "order-invoiced",
  "newScenarioState": "order-shipped",
  "request": {
    "method": "PUT",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {}
  }
}
```

- [ ] **Step 9: docs/wiremock/mappings/trendyol/order/get-shipping-label.json**

```json
{
  "priority": 5,
  "request": {
    "method": "GET",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/shipping-label$"
  },
  "response": {
    "status": 200,
    "headers": {
      "Content-Type": "application/pdf",
      "Content-Disposition": "attachment; filename=\"label.pdf\""
    },
    "base64Body": "JVBERi0xLjAKJSUhUFMtQWRvYmUtMy4wCjEgMCBvYmoKPDwgL1R5cGUgL0NhdGFsb2cgL1BhZ2VzIDIgMCBSID4+CmVuZG9iagoyIDAgb2JqCjw8IC9UeXBlIC9QYWdlcyAvS2lkcyBbMyAwIFJdIC9Db3VudCAxID4+CmVuZG9iagozIDAgb2JqCjw8IC9UeXBlIC9QYWdlID4+CmVuZG9iagp4cmVmCjAgNAowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMDkgMDAwMDAgbiAKMDAwMDAwMDA1OCAwMDAwMCBuIAowMDAwMDAwMTE1IDAgMDAwMDAgbiAKdHJhaWxlcgo8PCAvU2l6ZSA0IC9Sb290IDEgMCBSID4+CnN0YXJ0eHJlZgoxNDQKJSVFT0Y="
  }
}
```

- [ ] **Step 10: docs/wiremock/__files/trendyol/invoice/link-response.json**

```json
{ "success": true }
```

- [ ] **Step 11: docs/wiremock/mappings/trendyol/invoice/create-link.json**

Scenario: `Started` veya `order-invoiced` yok — her state'de çalışır, state'i `order-invoiced`'a çeker:

```json
{
  "priority": 5,
  "scenarioName": "order-lifecycle",
  "newScenarioState": "order-invoiced",
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/seller-invoice-links$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/invoice/link-response.json"
  }
}
```

- [ ] **Step 12: docs/wiremock/mappings/trendyol/invoice/delete-link.json**

```json
{
  "priority": 5,
  "request": {
    "method": "DELETE",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/seller-invoice-links/[^/]+/customers/[0-9]+$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {}
  }
}
```

- [ ] **Step 13: docs/wiremock/mappings/trendyol/invoice/upload-pdf.json**

Multipart POST — sadece method + URL eşleşmesi yeterli:

```json
{
  "priority": 5,
  "request": {
    "method": "POST",
    "urlPathPattern": "/integration/order/sellers/[0-9]+/shipment-packages/[0-9]+/invoice$"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "jsonBody": {}
  }
}
```

- [ ] **Step 14: Commit**

```bash
git add docs/wiremock/mappings/trendyol/order/
git add docs/wiremock/mappings/trendyol/invoice/
git add docs/wiremock/__files/trendyol/order/
git add docs/wiremock/__files/trendyol/invoice/
git commit -m "feat(wiremock/trendyol): order + invoice mappings + order-lifecycle scenario

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 7: Global 429 + Full Build Verification

**Files:**
- Create: `docs/wiremock/mappings/trendyol/_global-429.json`

- [ ] **Step 1: docs/wiremock/mappings/trendyol/_global-429.json**

Rate-limit simülasyonu — priority 99 (en düşük), sadece özel header ile tetiklenir. Normal dev akışını bozmaz:

```json
{
  "priority": 99,
  "request": {
    "method": "ANY",
    "urlPathPattern": "/integration/.*",
    "headers": {
      "X-Simulate-RateLimit": { "equalTo": "true" }
    }
  },
  "response": {
    "status": 429,
    "headers": {
      "Content-Type": "application/json",
      "Retry-After": "10"
    },
    "jsonBody": {
      "errors": [{ "code": "RATE_LIMIT_EXCEEDED", "message": "Too many requests. Retry after 10 seconds." }]
    }
  }
}
```

- [ ] **Step 2: Build doğrula**

```bash
dotnet build Entegrasyon.sln
```

Beklenen: 0 errors, 0 warnings.

- [ ] **Step 3: Unit test suite**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Beklenen: tüm testler green.

- [ ] **Step 4: Commit**

```bash
git add docs/wiremock/mappings/trendyol/_global-429.json
git commit -m "feat(wiremock/trendyol): global 429 rate-limit mapping (trigger-only)

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 8: Deploy + Doğrulama

- [ ] **Step 1: develop'e push**

```bash
git push origin develop
```

CI pipeline: test → build-push → deploy (wiremock sync dahil). ~3-5 dakika.

- [ ] **Step 2: CI loglarını izle**

Gitea UI'dan `develop` pipeline'ını aç. "Sync WireMock stubs to stack" adımının çıktısını kontrol et: `WireMock stubs synced`.

- [ ] **Step 3: WireMock admin'den mapping sayısını doğrula**

WireMock admin UI `http://192.168.1.78:8091/__admin/mappings` → Trendyol mapping sayısı artmış olmalı (önceki 5'ten ~25'e).

Veya:
```bash
curl -s http://192.168.1.78:8091/__admin/mappings | python3 -c "
import json,sys
data=json.load(sys.stdin)
ty=[m for m in data['mappings'] if 'trendyol' in m.get('response',{}).get('bodyFileName','') or 'integration' in m.get('request',{}).get('urlPathPattern','') or 'product' in m.get('request',{}).get('urlPathPattern','')]
print(f'Trendyol mappings: {len(ty)}')
"
```

Beklenen: 20+ mapping.

- [ ] **Step 4: Ürün yayınlama flow'unu doğrula (dev UI)**

1. `http://192.168.1.78:8085` → Ürünler → bir ürün seç → Trendyol'a Gönder
2. WireMock log: `POST /integration/product/sellers/123456/v2/products` → 200
3. Birkaç dakika bekle → `TrendyolBatchStatusPollingService` batch'i kontrol eder
4. WireMock log: `GET .../batch-requests/batch-test-001` → ilk seferinde IN_PROGRESS, ikinci seferinde COMPLETED
5. Ürün `Published` statüsüne geçmeli

- [ ] **Step 5: Sipariş akışını doğrula**

1. Siparişler sayfasına git → `GET /integration/order/sellers/123456/orders` → 200 (Created siparişler)
2. Bir siparişe fatura linki ekle → `POST .../seller-invoice-links` → 200, scenario `order-invoiced`'a geçer
3. Siparişler sayfasını yenile → aynı sipariş artık "Picking" statüsünde görünmeli
4. Kargo etiketi al → `GET .../shipping-label` → 200 (PDF bytes)
5. Kargoya ver → `PUT .../shipment-packages/99001` → 200, scenario `order-shipped`'a geçer

- [ ] **Step 6: Container log'larında hata kontrolü**

```bash
ssh server "docker logs entegrasyon-mvc-dev --tail 50 2>&1 | grep -E 'ERROR|Exception|WireMock'"
```

Beklenen: WireMock 404/unmapped uyarısı yok.

# Satış Modülü Yeniden Tasarım — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sale entity'sini merkeze alarak POS terminali, satış listesi, ödeme yöntemi yapılandırma, satış detay, iade akışı ve X/Z raporlarını yeniden tasarlamak.

**Architecture:** Mevcut Sale + SaleItem korunur, SalePayment (split payment), PaymentMethodDefinition (yapılandırılabilir), SaleReturn + SaleReturnItem (kısmi iade) eklenir. POSTransaction sadeleştirilir — ödeme bilgisi SalePayment'a taşınır. UI'da fiyatlar KDV dahil gösterilir, DB'de KDV hariç kalır.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (PostgreSQL), HTMX, Tabler UI, Mapperly, FluentValidation, xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-13-sales-pos-redesign.md`

---

## File Structure

### New Files

| File | Responsibility |
|------|---------------|
| `Entity/Sales/SaleSource.cs` | SaleSource enum (POS, Manual) |
| `Entity/Sales/SaleStatus.cs` | SaleStatus enum (Completed, PartialReturn, FullReturn, Cancelled) |
| `Entity/Sales/ReturnStatus.cs` | ReturnStatus enum (Pending, Approved, Rejected) |
| `Entity/Sales/SalePayment.cs` | Split payment entity |
| `Entity/Sales/PaymentMethodDefinition.cs` | Yapılandırılabilir ödeme yöntemi entity |
| `Entity/Sales/SaleReturn.cs` | İade ana kaydı entity |
| `Entity/Sales/SaleReturnItem.cs` | İade kalemi entity |
| `Entity/Dtos/Sale/SalePaymentDto.cs` | Ödeme DTO |
| `Entity/Dtos/Sale/SaleDetailDto.cs` | Satış detay DTO'ları |
| `Entity/Dtos/Sale/SaleReturnDtos.cs` | İade DTO'ları |
| `Entity/Dtos/Sale/SaleSummaryDto.cs` | Özet kartlar DTO |
| `Entity/Dtos/POS/POSReportDto.cs` | X/Z rapor DTO |
| `DataAccess/.../SalePaymentEntityConfiguration.cs` | SalePayment EF config |
| `DataAccess/.../PaymentMethodDefinitionEntityConfiguration.cs` | PMD EF config |
| `DataAccess/.../SaleReturnEntityConfiguration.cs` | SaleReturn EF config |
| `DataAccess/.../SaleReturnItemEntityConfiguration.cs` | SaleReturnItem EF config |
| `Business/Abstract/IPaymentMethodManager.cs` | Ödeme yöntemi yönetim interface |
| `Business/Abstract/ISaleReturnManager.cs` | İade yönetim interface |
| `Business/Concrete/PaymentMethodManager.cs` | Ödeme yöntemi yönetim servisi |
| `Business/Concrete/SaleReturnManager.cs` | İade yönetim servisi |
| `Business/Validation/Sale/CreateSaleReturnValidator.cs` | İade validasyonu |
| `MVC/Features/Sales/Views/Detail.cshtml` | Satış detay sayfası |
| `MVC/Features/Sales/Views/Print.cshtml` | Fiş yazdırma |
| `MVC/Features/Sales/Views/Partials/_SaleSummaryCards.cshtml` | Özet kartlar partial |
| `MVC/Features/Sales/Views/Partials/_SaleReturnDialog.cshtml` | İade modal |
| `MVC/Features/Settings/Views/PaymentMethods.cshtml` | Ödeme yöntemi ayarları |
| `MVC/Features/POS/Views/XReport.cshtml` | X raporu |
| `MVC/Features/POS/Views/ZReport.cshtml` | Z raporu |
| `Test/.../SalePaymentTests.cs` | SalePayment unit testleri |
| `Test/.../PaymentMethodManagerTests.cs` | Ödeme yöntemi testleri |
| `Test/.../SaleReturnManagerTests.cs` | İade testleri |
| `Test/.../POSReportTests.cs` | X/Z rapor testleri |

### Modified Files

| File | Changes |
|------|---------|
| `Entity/Sales/Sale.cs` | SaleNumber, SaleDate, SaleSource, SaleStatus, Note, Payments, Returns navigation |
| `Entity/Sales/SaleItem.cs` | Barcode, ProductTitle, ReturnedQuantity |
| `Entity/POS/POSTransaction.cs` | PaymentMethod enum kaldır, CardAuthCode kaldır |
| `Entity/Dtos/Sale/MakeSaleDto.cs` | SaleSource, Note, Payments listesi |
| `Entity/Dtos/Sale/SaleListDetailDto.cs` | Yeni alanlar (SaleNumber, SaleSource, SaleStatus, PaymentMethods) |
| `Entity/Dtos/Sale/SalePageableDto.cs` | Yeni filtreler (SaleSource, SaleStatus) |
| `DataAccess/.../SaleEntityConfiguration.cs` | Yeni alanlar, index'ler |
| `DataAccess/.../SaleItemEntityConfiguration.cs` | Yeni alanlar |
| `DataAccess/.../POSTransactionEntityConfiguration.cs` | Kaldırılan alanlar |
| `DataAccess/.../IntegrationDbContext.cs` | Yeni DbSet'ler |
| `Business/Abstract/ISaleManager.cs` | GetSaleDetailAsync, CancelSaleAsync, GetSalesSummaryAsync |
| `Business/Concrete/SaleManager.cs` | MakeSale güncelle, yeni metodlar |
| `Business/Validation/Sale/MakeSaleValidator.cs` | Payments validasyonu |
| `Business/Mappers/SaleMapper.cs` | Yeni mapping'ler |
| `ApplicationBootstrap/ApplicationDependencyExtension.cs` | Yeni DI kayıtları |
| `MVC/Features/Sales/SaleController.cs` | Detail, Cancel, Return endpoint'leri |
| `MVC/Features/Sales/ViewModels/SaleEntryVm.cs` | KDV dahil computed properties |
| `MVC/Features/Sales/Views/Index.cshtml` | Özet kartlar, filtreler |
| `MVC/Features/Sales/Views/Create.cshtml` | KDV dahil gösterim |
| `MVC/Features/Sales/Views/Partials/_SaleCart.cshtml` | KDV dahil gösterim |
| `MVC/Features/Sales/Views/Partials/_SaleTable.cshtml` | Yeni sütunlar |
| `MVC/Features/POS/POSController.cs` | X/Z rapor, dinamik ödeme |
| `MVC/Features/POS/ViewModels/POSTerminalVm.cs` | KDV dahil, müşteri |
| `MVC/Features/POS/Views/Index.cshtml` | KDV dahil, müşteri seçimi |
| `MVC/Features/POS/Views/Partials/_POSCart.cshtml` | KDV dahil |
| `MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml` | Dinamik ödeme |
| `MVC/Features/Settings/SettingsController.cs` | PaymentMethods endpoint'leri |

---

## Task 1: Yeni Enum'lar

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/SaleSource.cs`
- Create: `Application/Entegrasyon.Entity/Sales/SaleStatus.cs`
- Create: `Application/Entegrasyon.Entity/Sales/ReturnStatus.cs`

- [ ] **Step 1: SaleSource enum oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/SaleSource.cs
namespace Entegrasyon.Entity.Sales;

public enum SaleSource
{
    POS = 1,
    Manual = 2
}
```

- [ ] **Step 2: SaleStatus enum oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/SaleStatus.cs
namespace Entegrasyon.Entity.Sales;

public enum SaleStatus
{
    Completed = 1,
    PartialReturn = 2,
    FullReturn = 3,
    Cancelled = 4
}
```

- [ ] **Step 3: ReturnStatus enum oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/ReturnStatus.cs
namespace Entegrasyon.Entity.Sales;

public enum ReturnStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
```

- [ ] **Step 4: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/SaleSource.cs Application/Entegrasyon.Entity/Sales/SaleStatus.cs Application/Entegrasyon.Entity/Sales/ReturnStatus.cs
git commit -m "feat(entity): SaleSource, SaleStatus, ReturnStatus enum'ları"
```

---

## Task 2: PaymentMethodDefinition Entity

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/PaymentMethodDefinition.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/PaymentMethodDefinitionEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

- [ ] **Step 1: PaymentMethodDefinition entity oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/PaymentMethodDefinition.cs
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Shared;

namespace Entegrasyon.Entity.Sales;

public class PaymentMethodDefinition : BaseEntity
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(50)]
    public string SystemCode { get; set; } = "";

    [StringLength(50)]
    public string Icon { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public decimal? CommissionRate { get; set; }
    public bool RequiresAuthCode { get; set; }
    public bool RequiresCashInput { get; set; }
    public int TenantId { get; set; }
}
```

- [ ] **Step 2: EF Configuration oluştur**

```csharp
// Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/PaymentMethodDefinitionEntityConfiguration.cs
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class PaymentMethodDefinitionEntityConfiguration : IEntityTypeConfiguration<PaymentMethodDefinition>
{
    public void Configure(EntityTypeBuilder<PaymentMethodDefinition> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.CommissionRate).HasColumnType("numeric(5,2)");

        builder.HasIndex(x => new { x.TenantId, x.SystemCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
```

- [ ] **Step 3: IntegrationDbContext'e DbSet ekle**

`IntegrationDbContext.cs` dosyasına ekle:

```csharp
public DbSet<PaymentMethodDefinition> PaymentMethodDefinitions { get; set; } = null!;
```

- [ ] **Step 4: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/PaymentMethodDefinition.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/PaymentMethodDefinitionEntityConfiguration.cs
git commit -m "feat(entity): PaymentMethodDefinition yapılandırılabilir ödeme yöntemi entity"
```

---

## Task 3: SalePayment Entity

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/SalePayment.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SalePaymentEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

- [ ] **Step 1: SalePayment entity oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/SalePayment.cs
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Shared;

namespace Entegrasyon.Entity.Sales;

public class SalePayment : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethodDefinition PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? CashReceived { get; set; }
    public decimal? ChangeGiven { get; set; }

    [StringLength(100)]
    public string? CardAuthCode { get; set; }

    [StringLength(200)]
    public string? TransactionRef { get; set; }

    public DateTimeOffset PaidAt { get; set; }
}
```

- [ ] **Step 2: EF Configuration oluştur**

```csharp
// Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SalePaymentEntityConfiguration.cs
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SalePaymentEntityConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.CashReceived).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ChangeGiven).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Sale)
            .WithMany(s => s.Payments)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleId);
    }
}
```

- [ ] **Step 3: IntegrationDbContext'e DbSet ekle**

```csharp
public DbSet<SalePayment> SalePayments { get; set; } = null!;
```

- [ ] **Step 4: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/SalePayment.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SalePaymentEntityConfiguration.cs
git commit -m "feat(entity): SalePayment split payment entity"
```

---

## Task 4: SaleReturn + SaleReturnItem Entity'leri

**Files:**
- Create: `Application/Entegrasyon.Entity/Sales/SaleReturn.cs`
- Create: `Application/Entegrasyon.Entity/Sales/SaleReturnItem.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnEntityConfiguration.cs`
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnItemEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

- [ ] **Step 1: SaleReturn entity oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/SaleReturn.cs
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Identity;
using Entegrasyon.Entity.Shared;

namespace Entegrasyon.Entity.Sales;

public class SaleReturn : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public DateTimeOffset ReturnDate { get; set; }
    public Guid ReturnedByUserId { get; set; }
    public ApplicationUser ReturnedBy { get; set; } = null!;
    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public ReturnStatus ReturnStatus { get; set; }

    [StringLength(500)]
    public string ReturnReason { get; set; } = "";

    public int? RefundPaymentMethodId { get; set; }
    public PaymentMethodDefinition? RefundPaymentMethod { get; set; }
    public decimal RefundAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public ICollection<SaleReturnItem> Items { get; set; } = [];
}
```

- [ ] **Step 2: SaleReturnItem entity oluştur**

```csharp
// Application/Entegrasyon.Entity/Sales/SaleReturnItem.cs
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Shared;

namespace Entegrasyon.Entity.Sales;

public class SaleReturnItem : BaseEntity
{
    public long Id { get; set; }
    public long SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;
    public Guid SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
```

- [ ] **Step 3: SaleReturn EF Configuration**

```csharp
// Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnEntityConfiguration.cs
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleReturnEntityConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.RefundAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Sale)
            .WithMany(s => s.Returns)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnedBy)
            .WithMany()
            .HasForeignKey(x => x.ReturnedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedBy)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RefundPaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.RefundPaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.SaleReturn)
            .HasForeignKey(x => x.SaleReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SaleId);
    }
}
```

- [ ] **Step 4: SaleReturnItem EF Configuration**

```csharp
// Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnItemEntityConfiguration.cs
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleReturnItemEntityConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.SaleItem)
            .WithMany()
            .HasForeignKey(x => x.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: IntegrationDbContext'e DbSet'ler ekle**

```csharp
public DbSet<SaleReturn> SaleReturns { get; set; } = null!;
public DbSet<SaleReturnItem> SaleReturnItems { get; set; } = null!;
```

- [ ] **Step 6: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/SaleReturn.cs Application/Entegrasyon.Entity/Sales/SaleReturnItem.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnEntityConfiguration.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleReturnItemEntityConfiguration.cs
git commit -m "feat(entity): SaleReturn + SaleReturnItem iade entity'leri"
```

---

## Task 5: Sale + SaleItem Entity Güncellemeleri

**Files:**
- Modify: `Application/Entegrasyon.Entity/Sales/Sale.cs`
- Modify: `Application/Entegrasyon.Entity/Sales/SaleItem.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleEntityConfiguration.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleItemEntityConfiguration.cs`

- [ ] **Step 1: Sale entity'ye yeni alanlar ekle**

Mevcut `Sale.cs`'i güncelle — yeni property'leri mevcut property'lerin altına ekle:

```csharp
// Mevcut alanlara EK olarak:
using System.ComponentModel.DataAnnotations;

// Sale sınıfının içine ekle:
[StringLength(50)]
public string SaleNumber { get; set; } = "";

public DateTimeOffset SaleDate { get; set; }
public SaleSource SaleSource { get; set; }
public SaleStatus SaleStatus { get; set; } = SaleStatus.Completed;

[StringLength(500)]
public string? Note { get; set; }

// Navigation property'ler
public ICollection<SalePayment> Payments { get; set; } = [];
public ICollection<SaleReturn> Returns { get; set; } = [];
```

Not: `sealed` modifier'ı kaldırılmalı çünkü navigation collection'lar lazy loading ile uyumlu olmalı. Alternatif olarak `sealed` kalabilir (no-tracking zaten kullanılıyor).

- [ ] **Step 2: SaleItem entity'ye yeni alanlar ekle**

Mevcut `SaleItem.cs`'e ekle:

```csharp
using System.ComponentModel.DataAnnotations;

// SaleItem sınıfının içine ekle:
[StringLength(100)]
public string Barcode { get; set; } = "";

[StringLength(300)]
public string ProductTitle { get; set; } = "";

public int ReturnedQuantity { get; set; }
```

- [ ] **Step 3: SaleEntityConfiguration güncelle**

```csharp
public void Configure(EntityTypeBuilder<Sale> builder)
{
    builder.HasQueryFilter(x => !x.IsDeleted);

    builder.HasIndex(x => x.SaleNumber).IsUnique();
    builder.HasIndex(x => x.SaleDate);
    builder.HasIndex(x => x.SaleSource);
    builder.HasIndex(x => x.SaleStatus);
}
```

- [ ] **Step 4: SaleItemEntityConfiguration güncelle**

Mevcut configuration'a ek olarak:

```csharp
public void Configure(EntityTypeBuilder<SaleItem> builder)
{
    builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
    builder.HasQueryFilter(x => !x.IsDeleted);

    // Yeni: ReturnedQuantity varsayılan değer
    builder.Property(x => x.ReturnedQuantity).HasDefaultValue(0);
}
```

- [ ] **Step 5: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Entity/Sales/Sale.cs Application/Entegrasyon.Entity/Sales/SaleItem.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleEntityConfiguration.cs Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/SaleItemEntityConfiguration.cs
git commit -m "feat(entity): Sale + SaleItem yeni alanlar (SaleNumber, SaleStatus, Barcode, ReturnedQuantity)"
```

---

## Task 6: POSTransaction Sadeleştirme

**Files:**
- Modify: `Application/Entegrasyon.Entity/POS/POSTransaction.cs`
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/EntityConfigurations/POSTransactionEntityConfiguration.cs`

- [ ] **Step 1: POSTransaction'dan PaymentMethod ve CardAuthCode kaldır**

`POSTransaction.cs`'i güncelle — `PaymentMethod` property'sini ve `CardAuthCode`'u kaldır:

```csharp
// Application/Entegrasyon.Entity/POS/POSTransaction.cs
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Shared;

namespace Entegrasyon.Entity.POS;

public class POSTransaction : BaseEntity
{
    public long Id { get; set; }
    public long POSSessionId { get; set; }
    public Guid SaleId { get; set; }
    // PaymentMethod enum KALDIRILDI — ödeme bilgisi artık SalePayment'ta
    // CardAuthCode KALDIRILDI — SalePayment'a taşındı
    public decimal CashReceived { get; set; }
    public decimal ChangeGiven { get; set; }
    public DateTimeOffset TransactionAt { get; set; }

    public POSSession POSSession { get; set; } = null!;
    public Sale Sale { get; set; } = null!;
}
```

- [ ] **Step 2: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: PaymentMethod ve CardAuthCode kullanan yerler hata verecek. Bu hatalar Task 10'da (SaleManager güncellemesi) ve Task 17'de (POS controller güncellemesi) düzeltilecek. Şimdilik build hatası bekleniyor — entity değişikliğini commit et.

Not: Eğer build hataları çok fazlaysa, bu task'ı Task 10 ile birleştir. Alternatif olarak, `PaymentMethod` property'sini `[Obsolete]` ile işaretle ve migration'da kaldır.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Entity/POS/POSTransaction.cs
git commit -m "refactor(entity): POSTransaction PaymentMethod + CardAuthCode kaldır — SalePayment'a taşındı"
```

---

## Task 7: EF Core Migration + Seed Data

**Files:**
- Create: Migration dosyası (auto-generated)
- Modify: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Contexts/IntegrationDbContext.cs`

**Önemli:** Bu task, Task 1-6'nın tamamlanmasını ve build'in başarılı olmasını gerektirir. Build hataları varsa önce düzelt.

- [ ] **Step 1: Migration oluştur**

Run:
```bash
dotnet ef migrations add SalesModuleRedesign -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```

- [ ] **Step 2: Migration dosyasını incele**

Migration dosyasında şunlar olmalı:
- PaymentMethodDefinitions tablosu oluşturma
- SalePayments tablosu oluşturma
- SaleReturns tablosu oluşturma
- SaleReturnItems tablosu oluşturma
- Sales tablosuna SaleNumber, SaleDate, SaleSource, SaleStatus, Note sütunları
- SaleItems tablosuna Barcode, ProductTitle, ReturnedQuantity sütunları
- POSTransactions tablosundan PaymentMethod, CardAuthCode kaldırma

Migration dosyasına **seed data** ve **veri migration** SQL'i ekle:

```csharp
// Up() metodunun sonuna seed data ekle:
migrationBuilder.Sql("""
    -- Varsayılan ödeme yöntemleri seed
    INSERT INTO "PaymentMethodDefinitions" ("Name", "SystemCode", "Icon", "IsActive", "SortOrder", "CommissionRate", "RequiresAuthCode", "RequiresCashInput", "TenantId", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt")
    VALUES
        ('Nakit', 'Cash', 'ti ti-cash', true, 1, NULL, false, true, 1, false, '0001-01-01', NOW(), NOW()),
        ('Kredi Kartı', 'CreditCard', 'ti ti-credit-card', true, 2, NULL, true, false, 1, false, '0001-01-01', NOW(), NOW()),
        ('Banka Kartı', 'DebitCard', 'ti ti-credit-card', true, 3, NULL, true, false, 1, false, '0001-01-01', NOW(), NOW()),
        ('Yemek Kartı', 'MealCard', 'ti ti-tools-kitchen-2', true, 4, NULL, true, false, 1, false, '0001-01-01', NOW(), NOW()),
        ('Havale/EFT', 'BankTransfer', 'ti ti-building-bank', true, 5, NULL, true, false, 1, false, '0001-01-01', NOW(), NOW());

    -- Mevcut Sale kayıtlarını güncelle
    UPDATE "Sales"
    SET "SaleStatus" = 1,  -- Completed
        "SaleSource" = 1,  -- POS
        "SaleDate" = "CreatedAt",
        "SaleNumber" = 'LEGACY-' || "Id"::text
    WHERE "SaleNumber" = '' OR "SaleNumber" IS NULL;

    -- Mevcut POSTransaction'lardan SalePayment kayıtları oluştur
    INSERT INTO "SalePayments" ("SaleId", "PaymentMethodId", "Amount", "CashReceived", "ChangeGiven", "CardAuthCode", "PaidAt", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt")
    SELECT
        pt."SaleId",
        CASE pt."PaymentMethod"
            WHEN 1 THEN (SELECT "Id" FROM "PaymentMethodDefinitions" WHERE "SystemCode" = 'Cash' LIMIT 1)
            WHEN 2 THEN (SELECT "Id" FROM "PaymentMethodDefinitions" WHERE "SystemCode" = 'CreditCard' LIMIT 1)
            WHEN 3 THEN (SELECT "Id" FROM "PaymentMethodDefinitions" WHERE "SystemCode" = 'DebitCard' LIMIT 1)
            WHEN 5 THEN (SELECT "Id" FROM "PaymentMethodDefinitions" WHERE "SystemCode" = 'MealCard' LIMIT 1)
            ELSE (SELECT "Id" FROM "PaymentMethodDefinitions" WHERE "SystemCode" = 'Cash' LIMIT 1)
        END,
        (SELECT SUM(si."UnitPrice" * si."Quantity") FROM "SaleItems" si WHERE si."SaleId" = pt."SaleId" AND si."IsDeleted" = false),
        pt."CashReceived",
        pt."ChangeGiven",
        pt."CardAuthCode",
        pt."TransactionAt",
        false, '0001-01-01', NOW(), NOW()
    FROM "POSTransactions" pt
    WHERE pt."IsDeleted" = false;
""");
```

- [ ] **Step 3: Migration uygula**

Run:
```bash
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```

- [ ] **Step 4: Model snapshot doğrula**

Run:
```bash
dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```
Expected: No pending model changes

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.DataAccess/
git commit -m "feat(migration): SalesModuleRedesign — yeni tablolar, seed data, veri migrasyonu"
```

---

## Task 8: Yeni DTO'lar

**Files:**
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/SalePaymentDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/SaleReturnDtos.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/Sale/SaleSummaryDto.cs`
- Create: `Application/Entegrasyon.Entity/Dtos/POS/POSReportDto.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Sale/SaleListDetailDto.cs`
- Modify: `Application/Entegrasyon.Entity/Dtos/Sale/SalePageableDto.cs`

- [ ] **Step 1: SalePaymentDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SalePaymentDto.cs
namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record SalePaymentDto(
    int PaymentMethodId,
    decimal Amount,
    decimal? CashReceived,
    string? CardAuthCode);
```

- [ ] **Step 2: MakeSaleDto güncelle**

Mevcut dosyayı güncelle — SaleSource, Note, Payments ekle:

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/MakeSaleDto.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record MakeSaleDto(
    Guid SalePersonId,
    int CustomerId,
    double GeneralDiscount,
    int BranchOfficeId,
    SaleSource SaleSource,
    string? Note,
    IEnumerable<SaleItemDto> SaleItems,
    List<SalePaymentDto> Payments);

public sealed record SaleItemDto(
    Guid ProductVariantId,
    double TaxPercentage,
    double DiscountPercent,
    decimal UnitPrice,
    int Quantity,
    string DiscountVoucherCode);
```

- [ ] **Step 3: SaleListDetailDto güncelle**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SaleListDetailDto.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleListDetailDto(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SaleDate,
    SaleSource SaleSource,
    SaleStatus SaleStatus,
    string? CustomerFullName,
    string SalePersonFullName,
    int SaleItemVarietyCount,
    int SaleItemCount,
    decimal GrandTotal,
    List<string> PaymentMethods);
```

- [ ] **Step 4: SalePageableDto güncelle**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SalePageableDto.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SalePageableDto(
    int? CustomerId,
    DateTimeOffset? DateBetweenStart,
    DateTimeOffset? DateBetweenEnd,
    Guid SalePersonId,
    SaleSource? SaleSource,
    SaleStatus? SaleStatus,
    string FullTextSearchKey,
    int PageIndex = 0,
    int PageSize = 50
) : SearchablePageDto(FullTextSearchKey, PageIndex, PageSize);
```

- [ ] **Step 5: SaleDetailDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SaleDetailDto.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleDetailDto
{
    public Guid Id { get; init; }
    public string SaleNumber { get; init; } = "";
    public DateTimeOffset SaleDate { get; init; }
    public SaleSource SaleSource { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public string? CustomerName { get; init; }
    public string SalePersonName { get; init; } = "";
    public string BranchOfficeName { get; init; } = "";
    public string? Note { get; init; }
    public decimal SubTotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public double GeneralDiscount { get; init; }
    public List<SaleDetailItemDto> Items { get; init; } = [];
    public List<SaleDetailPaymentDto> Payments { get; init; } = [];
    public List<VatSummaryLineDto> VatSummary { get; init; } = [];
    public List<SaleReturnSummaryDto> Returns { get; init; } = [];
}

public record SaleDetailItemDto
{
    public Guid Id { get; init; }
    public string ProductTitle { get; init; } = "";
    public string Barcode { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPriceWithVat { get; init; }
    public decimal VatRate { get; init; }
    public decimal LineTotalWithVat { get; init; }
    public int ReturnedQuantity { get; init; }
}

public record SaleDetailPaymentDto
{
    public string PaymentMethodName { get; init; } = "";
    public string PaymentMethodIcon { get; init; } = "";
    public decimal Amount { get; init; }
    public string? CardAuthCode { get; init; }
    public DateTimeOffset PaidAt { get; init; }
}

public record VatSummaryLineDto(
    decimal VatRate,
    decimal TaxBase,
    decimal VatAmount,
    decimal Total);
```

- [ ] **Step 6: SaleReturnDtos oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SaleReturnDtos.cs
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record CreateSaleReturnDto(
    Guid SaleId,
    Guid ReturnedByUserId,
    string ReturnReason,
    int? RefundPaymentMethodId,
    string? Note,
    List<SaleReturnItemDto> Items);

public sealed record SaleReturnItemDto(
    Guid SaleItemId,
    int Quantity,
    string? Reason);

public record SaleReturnSummaryDto
{
    public long Id { get; init; }
    public DateTimeOffset ReturnDate { get; init; }
    public ReturnStatus ReturnStatus { get; init; }
    public string ReturnReason { get; init; } = "";
    public decimal RefundAmount { get; init; }
    public string ReturnedByName { get; init; } = "";
    public int ItemCount { get; init; }
}
```

- [ ] **Step 7: SaleSummaryDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/Sale/SaleSummaryDto.cs
namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleSummaryDto(
    decimal TotalSales,
    int SaleCount,
    decimal AverageBasket,
    decimal TotalReturns);
```

- [ ] **Step 8: POSReportDto oluştur**

```csharp
// Application/Entegrasyon.Entity/Dtos/POS/POSReportDto.cs
using Entegrasyon.Entity.Dtos.Sale;

namespace Entegrasyon.Entity.Dtos.POS;

public record POSReportDto
{
    public long SessionId { get; init; }
    public string CashierName { get; init; } = "";
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public decimal OpeningCash { get; init; }
    public int TransactionCount { get; init; }
    public decimal TotalSales { get; init; }
    public decimal TotalReturns { get; init; }
    public decimal NetSales { get; init; }
    public List<PaymentMethodSummaryDto> PaymentBreakdown { get; init; } = [];
    public List<VatSummaryLineDto> VatBreakdown { get; init; } = [];
    public decimal ExpectedCash { get; init; }
    public decimal? ActualCash { get; init; }
    public decimal? CashDifference { get; init; }
}

public record PaymentMethodSummaryDto(
    string MethodName,
    int Count,
    decimal Total);
```

- [ ] **Step 9: Build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded (veya SaleManager'daki eski DTO referansları hata verebilir — Task 10'da düzeltilecek)

- [ ] **Step 10: Commit**

```bash
git add Application/Entegrasyon.Entity/Dtos/
git commit -m "feat(dto): satış modülü DTO'ları — SalePayment, SaleDetail, SaleReturn, POSReport"
```

---

## Task 9: IPaymentMethodManager + PaymentMethodManager

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPaymentMethodManager.cs`
- Create: `Application/Entegrasyon.Business/Concrete/PaymentMethodManager.cs`
- Create: `Test/Entegrasyon.Test/Business/PaymentMethodManagerTests.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/Business/PaymentMethodManagerTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Entegrasyon.Test.Business;

public class PaymentMethodManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();

    [Fact]
    public async Task GetActivePaymentMethodsAsync_ReturnsOnlyActiveMethodsForTenant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var setupContext = new IntegrationDbContext(options);
        setupContext.PaymentMethodDefinitions.AddRange(
            new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "Cash", IsActive = true, SortOrder = 1, TenantId = 1 },
            new PaymentMethodDefinition { Id = 2, Name = "Kredi Kartı", SystemCode = "CreditCard", IsActive = true, SortOrder = 2, TenantId = 1 },
            new PaymentMethodDefinition { Id = 3, Name = "Pasif", SystemCode = "Disabled", IsActive = false, SortOrder = 3, TenantId = 1 },
            new PaymentMethodDefinition { Id = 4, Name = "Diğer Tenant", SystemCode = "Other", IsActive = true, SortOrder = 1, TenantId = 2 }
        );
        await setupContext.SaveChangesAsync();

        _contextFactoryMock.Setup(x => x.CreateDbContextAsync(default))
            .ReturnsAsync(new IntegrationDbContext(options));

        var manager = new PaymentMethodManager(_contextFactoryMock.Object);

        // Act
        var result = await manager.GetActivePaymentMethodsAsync(tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(x => x.IsActive && x.TenantId == 1);
        result.Data.Should().BeInAscendingOrder(x => x.SortOrder);
    }

    [Fact]
    public async Task TogglePaymentMethodAsync_TogglesIsActive()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var setupContext = new IntegrationDbContext(options);
        setupContext.PaymentMethodDefinitions.Add(
            new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "Cash", IsActive = true, SortOrder = 1, TenantId = 1 }
        );
        await setupContext.SaveChangesAsync();

        _contextFactoryMock.Setup(x => x.CreateDbContextAsync(default))
            .ReturnsAsync(new IntegrationDbContext(options));

        var manager = new PaymentMethodManager(_contextFactoryMock.Object);

        // Act
        var result = await manager.TogglePaymentMethodAsync(1);

        // Assert
        result.Success.Should().BeTrue();

        await using var verifyContext = new IntegrationDbContext(options);
        var method = await verifyContext.PaymentMethodDefinitions.FindAsync(1);
        method!.IsActive.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Test çalıştır — FAIL beklenir**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PaymentMethodManagerTests"`
Expected: FAIL — PaymentMethodManager sınıfı henüz yok

- [ ] **Step 3: IPaymentMethodManager interface oluştur**

```csharp
// Application/Entegrasyon.Business/Abstract/IPaymentMethodManager.cs
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface IPaymentMethodManager
{
    Task<IDataResult<List<PaymentMethodDefinition>>> GetActivePaymentMethodsAsync(int tenantId);
    Task<IDataResult<List<PaymentMethodDefinition>>> GetAllPaymentMethodsAsync(int tenantId);
    Task<IResult> TogglePaymentMethodAsync(int id);
    Task<IResult> UpdatePaymentMethodAsync(int id, string name, string icon, decimal? commissionRate);
    Task<IResult> ReorderPaymentMethodsAsync(List<int> orderedIds);
    Task<IResult> CreatePaymentMethodAsync(string name, string systemCode, string icon, bool requiresAuthCode, bool requiresCashInput, int tenantId);
}
```

- [ ] **Step 4: PaymentMethodManager implement et**

```csharp
// Application/Entegrasyon.Business/Concrete/PaymentMethodManager.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class PaymentMethodManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IPaymentMethodManager
{
    public async Task<IDataResult<List<PaymentMethodDefinition>>> GetActivePaymentMethodsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return new SuccessDataResult<List<PaymentMethodDefinition>>(methods);
    }

    public async Task<IDataResult<List<PaymentMethodDefinition>>> GetAllPaymentMethodsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return new SuccessDataResult<List<PaymentMethodDefinition>>(methods);
    }

    public async Task<IResult> TogglePaymentMethodAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var method = await dbContext.PaymentMethodDefinitions.FindAsync(id);
        if (method is null)
            return new ErrorResult("Ödeme yöntemi bulunamadı.");

        method.IsActive = !method.IsActive;
        dbContext.PaymentMethodDefinitions.Update(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult(method.IsActive ? "Ödeme yöntemi aktifleştirildi." : "Ödeme yöntemi pasifleştirildi.");
    }

    public async Task<IResult> UpdatePaymentMethodAsync(int id, string name, string icon, decimal? commissionRate)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var method = await dbContext.PaymentMethodDefinitions.FindAsync(id);
        if (method is null)
            return new ErrorResult("Ödeme yöntemi bulunamadı.");

        method.Name = name;
        method.Icon = icon;
        method.CommissionRate = commissionRate;
        dbContext.PaymentMethodDefinitions.Update(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Ödeme yöntemi güncellendi.");
    }

    public async Task<IResult> ReorderPaymentMethodsAsync(List<int> orderedIds)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => orderedIds.Contains(x.Id))
            .ToListAsync();

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var method = methods.FirstOrDefault(x => x.Id == orderedIds[i]);
            if (method is not null)
                method.SortOrder = i + 1;
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sıralama güncellendi.");
    }

    public async Task<IResult> CreatePaymentMethodAsync(string name, string systemCode, string icon, bool requiresAuthCode, bool requiresCashInput, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var exists = await dbContext.PaymentMethodDefinitions
            .AnyAsync(x => x.TenantId == tenantId && x.SystemCode == systemCode);
        if (exists)
            return new ErrorResult("Bu sistem kodu zaten mevcut.");

        var maxSort = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0;

        var method = new PaymentMethodDefinition
        {
            Name = name,
            SystemCode = systemCode,
            Icon = icon,
            IsActive = true,
            SortOrder = maxSort + 1,
            RequiresAuthCode = requiresAuthCode,
            RequiresCashInput = requiresCashInput,
            TenantId = tenantId
        };

        dbContext.PaymentMethodDefinitions.Add(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Ödeme yöntemi oluşturuldu.");
    }
}
```

- [ ] **Step 5: DI kaydı ekle**

`ApplicationDependencyExtension.cs`'te `AddApplicationDependencies()` metoduna ekle:

```csharp
services.AddScoped<IPaymentMethodManager, PaymentMethodManager>();
```

- [ ] **Step 6: Test çalıştır — PASS beklenir**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PaymentMethodManagerTests"`
Expected: 2 tests passed

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPaymentMethodManager.cs Application/Entegrasyon.Business/Concrete/PaymentMethodManager.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs Test/Entegrasyon.Test/Business/PaymentMethodManagerTests.cs
git commit -m "feat(business): IPaymentMethodManager + PaymentMethodManager — ödeme yöntemi CRUD"
```

---

## Task 10: ISaleManager Güncellemesi + SaleManager Refactor

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/ISaleManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/SaleManager.cs`
- Modify: `Application/Entegrasyon.Business/Validation/Sale/MakeSaleValidator.cs`
- Modify: `Application/Entegrasyon.Business/Mappers/SaleMapper.cs`
- Create: `Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs`

- [ ] **Step 1: Failing test yaz — MakeSale with payments**

```csharp
// Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Core.Utilities.Validation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Entegrasyon.Test.Business;

public class SaleManagerMakeSaleTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IApplicationLogManager> _logManagerMock = new();
    private readonly Mock<IFluentValidator> _validatorMock = new();
    private readonly Mock<IOfficeStockManager> _stockManagerMock = new();
    private readonly SaleMapper _mapper = new();

    [Fact]
    public async Task MakeSale_WithSplitPayment_CreatesSaleAndPayments()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _contextFactoryMock.Setup(x => x.CreateDbContextAsync(default))
            .ReturnsAsync(new IntegrationDbContext(options));

        _stockManagerMock.Setup(x => x.DecreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        var manager = new SaleManager(
            _contextFactoryMock.Object, _logManagerMock.Object,
            _mapper, _validatorMock.Object, _stockManagerMock.Object);

        var variantId = Guid.NewGuid();
        var dto = new MakeSaleDto(
            SalePersonId: Guid.NewGuid(),
            CustomerId: 0,
            GeneralDiscount: 0,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: "Test satış",
            SaleItems: [new SaleItemDto(variantId, 20, 0, 100m, 2, "")],
            Payments: [
                new SalePaymentDto(PaymentMethodId: 1, Amount: 150m, CashReceived: 200m, CardAuthCode: null),
                new SalePaymentDto(PaymentMethodId: 2, Amount: 90m, CashReceived: null, CardAuthCode: "AUTH123")
            ]);

        // Act
        var result = await manager.MakeSale(dto);

        // Assert
        result.Success.Should().BeTrue();

        await using var verifyContext = new IntegrationDbContext(options);
        var sale = await verifyContext.Sales
            .Include(s => s.Payments)
            .FirstOrDefaultAsync();

        sale.Should().NotBeNull();
        sale!.SaleSource.Should().Be(SaleSource.POS);
        sale.SaleStatus.Should().Be(SaleStatus.Completed);
        sale.SaleNumber.Should().NotBeNullOrEmpty();
        sale.Note.Should().Be("Test satış");
        sale.Payments.Should().HaveCount(2);
        sale.Payments.Sum(p => p.Amount).Should().Be(240m);
    }
}
```

- [ ] **Step 2: Test çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleManagerMakeSaleTests"`
Expected: FAIL

- [ ] **Step 3: ISaleManager interface güncelle**

```csharp
// Application/Entegrasyon.Business/Abstract/ISaleManager.cs
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.Entity.Dtos.Sale;

namespace Entegrasyon.Business.Abstract;

public interface ISaleManager
{
    Task<IResult> MakeSale(MakeSaleDto dto);
    Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto);
    Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId);
    Task<IResult> CancelSaleAsync(Guid saleId, Guid cancelledByUserId);
    Task<IDataResult<SaleSummaryDto>> GetSalesSummaryAsync(SalePageableDto dto);
}
```

- [ ] **Step 4: MakeSaleValidator güncelle**

```csharp
// Application/Entegrasyon.Business/Validation/Sale/MakeSaleValidator.cs
using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.Sale;

public class MakeSaleValidator : AbstractValidator<MakeSaleDto>
{
    public MakeSaleValidator()
    {
        RuleFor(x => x.SalePersonId).NotEmpty();
        RuleFor(x => x.BranchOfficeId).GreaterThan(0);
        RuleFor(x => x.SaleItems)
            .NotNull().WithMessage("Lütfen satılacak ürün gönderin.")
            .Must(x => x.Any()).WithMessage("En az bir ürün eklemelisiniz.");
        RuleFor(x => x.Payments)
            .NotNull().WithMessage("Ödeme bilgisi gereklidir.")
            .Must(x => x.Count > 0).WithMessage("En az bir ödeme yöntemi seçmelisiniz.");
        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(x => x.PaymentMethodId).GreaterThan(0);
            p.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ödeme tutarı sıfırdan büyük olmalıdır.");
        });
    }
}
```

- [ ] **Step 5: SaleManager.MakeSale güncelle**

SaleManager.cs'teki `MakeSale` metodunu güncelle — SalePayment kayıtlarını oluştur, SaleNumber generate et, SaleDate/SaleSource/SaleStatus set et:

```csharp
public async Task<IResult> MakeSale(MakeSaleDto dto)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    await applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
    await fluentValidator.ValidateAndThrowAsync(dto);

    var sale = mapper.MapToEntity(dto);
    sale.SaleNumber = await GenerateSaleNumberAsync(dbContext);
    sale.SaleDate = DateTimeOffset.UtcNow;
    sale.SaleSource = dto.SaleSource;
    sale.SaleStatus = SaleStatus.Completed;
    sale.Note = dto.Note;

    // Denormalize ürün bilgileri
    foreach (var saleItem in sale.SaleItems)
    {
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == saleItem.ProductVariantId)
            .Select(v => new { v.Barcode, Title = v.Product!.Title })
            .FirstOrDefaultAsync();

        if (variant is not null)
        {
            saleItem.Barcode = variant.Barcode ?? "";
            saleItem.ProductTitle = variant.Title ?? "";
        }
    }

    // Atomic stok düşme
    foreach (var item in dto.SaleItems)
    {
        var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
            dto.BranchOfficeId, item.ProductVariantId, item.Quantity,
            StockMovementType.Sale, "Sale");

        if (!stockResult.Success)
            return new ErrorResult(stockResult.Message!);
    }

    // SalePayment kayıtları
    var now = DateTimeOffset.UtcNow;
    foreach (var paymentDto in dto.Payments)
    {
        sale.Payments.Add(new SalePayment
        {
            PaymentMethodId = paymentDto.PaymentMethodId,
            Amount = paymentDto.Amount,
            CashReceived = paymentDto.CashReceived,
            ChangeGiven = paymentDto.CashReceived.HasValue
                ? Math.Max(0, paymentDto.CashReceived.Value - paymentDto.Amount)
                : null,
            CardAuthCode = paymentDto.CardAuthCode,
            PaidAt = now
        });
    }

    dbContext.Sales.Add(sale);
    await dbContext.SaveChangesAsync();
    await applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
    return new SuccessResult(Messages.SaleSuccess);
}

private static async Task<string> GenerateSaleNumberAsync(IntegrationDbContext dbContext)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var prefix = $"S{today:yyyyMMdd}";

    var lastNumber = await dbContext.Sales
        .Where(s => s.SaleNumber.StartsWith(prefix))
        .OrderByDescending(s => s.SaleNumber)
        .Select(s => s.SaleNumber)
        .FirstOrDefaultAsync();

    var sequence = 1;
    if (lastNumber is not null && int.TryParse(lastNumber[prefix.Length..], out var lastSeq))
        sequence = lastSeq + 1;

    return $"{prefix}{sequence:D4}";
}
```

- [ ] **Step 6: SaleManager.GetSaleDetailAsync implement et**

```csharp
public async Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var sale = await dbContext.Sales
        .Include(s => s.SaleItems)
        .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
        .Include(s => s.Returns).ThenInclude(r => r.Items)
        .Include(s => s.Returns).ThenInclude(r => r.ReturnedBy)
        .Include(s => s.SalePerson)
        .Include(s => s.BranchOffice)
        .Include(s => s.Customer)
        .FirstOrDefaultAsync(s => s.Id == saleId);

    if (sale is null)
        return new ErrorDataResult<SaleDetailDto>("Satış bulunamadı.");

    var items = sale.SaleItems.Select(si =>
    {
        var lineTotal = si.UnitPrice * si.Quantity;
        var vatAmount = lineTotal * (decimal)si.TaxPercentage / 100m;
        return new SaleDetailItemDto
        {
            Id = si.Id,
            ProductTitle = si.ProductTitle,
            Barcode = si.Barcode,
            Quantity = si.Quantity,
            UnitPriceWithVat = Math.Round(si.UnitPrice * (1 + (decimal)si.TaxPercentage / 100m), 2),
            VatRate = (decimal)si.TaxPercentage,
            LineTotalWithVat = Math.Round(lineTotal + vatAmount, 2),
            ReturnedQuantity = si.ReturnedQuantity
        };
    }).ToList();

    var subTotal = sale.SaleItems.Sum(si => si.UnitPrice * si.Quantity);
    var vatTotal = sale.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (decimal)si.TaxPercentage / 100m);

    var vatSummary = sale.SaleItems
        .GroupBy(si => (decimal)si.TaxPercentage)
        .Select(g =>
        {
            var taxBase = g.Sum(si => si.UnitPrice * si.Quantity);
            var vatAmount = taxBase * g.Key / 100m;
            return new VatSummaryLineDto(g.Key, Math.Round(taxBase, 2), Math.Round(vatAmount, 2), Math.Round(taxBase + vatAmount, 2));
        })
        .OrderBy(v => v.VatRate)
        .ToList();

    var dto = new SaleDetailDto
    {
        Id = sale.Id,
        SaleNumber = sale.SaleNumber,
        SaleDate = sale.SaleDate,
        SaleSource = sale.SaleSource,
        SaleStatus = sale.SaleStatus,
        CustomerName = sale.Customer?.FullName,
        SalePersonName = $"{sale.SalePerson.Name} {sale.SalePerson.Surname}",
        BranchOfficeName = sale.BranchOffice.Name,
        Note = sale.Note,
        SubTotal = Math.Round(subTotal, 2),
        VatTotal = Math.Round(vatTotal, 2),
        GrandTotal = Math.Round(subTotal + vatTotal, 2),
        GeneralDiscount = sale.GeneralDiscount,
        Items = items,
        Payments = sale.Payments.Select(p => new SaleDetailPaymentDto
        {
            PaymentMethodName = p.PaymentMethod.Name,
            PaymentMethodIcon = p.PaymentMethod.Icon,
            Amount = p.Amount,
            CardAuthCode = p.CardAuthCode,
            PaidAt = p.PaidAt
        }).ToList(),
        VatSummary = vatSummary,
        Returns = sale.Returns.Select(r => new SaleReturnSummaryDto
        {
            Id = r.Id,
            ReturnDate = r.ReturnDate,
            ReturnStatus = r.ReturnStatus,
            ReturnReason = r.ReturnReason,
            RefundAmount = r.RefundAmount,
            ReturnedByName = $"{r.ReturnedBy.Name} {r.ReturnedBy.Surname}",
            ItemCount = r.Items.Count
        }).ToList()
    };

    return new SuccessDataResult<SaleDetailDto>(dto);
}
```

- [ ] **Step 7: SaleManager.GetSalesSummaryAsync implement et**

```csharp
public async Task<IDataResult<SaleSummaryDto>> GetSalesSummaryAsync(SalePageableDto dto)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var query = BuildSaleQuery(dbContext, dto);

    var totalSales = await query
        .Where(s => s.SaleStatus != SaleStatus.Cancelled)
        .SumAsync(s => s.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100m)));

    var saleCount = await query
        .Where(s => s.SaleStatus != SaleStatus.Cancelled)
        .CountAsync();

    var totalReturns = await dbContext.SaleReturns
        .Where(r => r.ReturnStatus == ReturnStatus.Approved)
        .Where(r => query.Select(s => s.Id).Contains(r.SaleId))
        .SumAsync(r => r.RefundAmount);

    var summary = new SaleSummaryDto(
        TotalSales: Math.Round(totalSales, 2),
        SaleCount: saleCount,
        AverageBasket: saleCount > 0 ? Math.Round(totalSales / saleCount, 2) : 0,
        TotalReturns: Math.Round(totalReturns, 2));

    return new SuccessDataResult<SaleSummaryDto>(summary);
}

// GetSalesPageable ve GetSalesSummaryAsync'in paylaştığı query builder
private static IQueryable<Sale> BuildSaleQuery(IntegrationDbContext dbContext, SalePageableDto dto)
{
    var query = dbContext.Sales.AsQueryable();

    if (dto.CustomerId.HasValue)
        query = query.Where(x => x.CustomerId == dto.CustomerId);
    if (dto.DateBetweenStart is not null)
        query = query.Where(x => x.SaleDate >= dto.DateBetweenStart);
    if (dto.DateBetweenEnd is not null)
        query = query.Where(x => x.SaleDate <= dto.DateBetweenEnd);
    if (dto.SalePersonId != Guid.Empty)
        query = query.Where(x => x.SalePersonId == dto.SalePersonId);
    if (dto.SaleSource.HasValue)
        query = query.Where(x => x.SaleSource == dto.SaleSource);
    if (dto.SaleStatus.HasValue)
        query = query.Where(x => x.SaleStatus == dto.SaleStatus);

    return query;
}
```

- [ ] **Step 8: SaleManager.GetSalesPageable güncelle**

Mevcut `GetSalesPageable`'ı yeni DTO ve filtreler ile güncelle — `BuildSaleQuery` kullan ve yeni `SaleListDetailDto` projection'ı:

```csharp
public async Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var query = BuildSaleQuery(dbContext, dto);

    int total = await query.CountAsync();
    var items = await query
        .OrderByDescending(x => x.SaleDate)
        .Skip(dto.PageIndex * dto.PageSize)
        .Take(dto.PageSize)
        .Select(x => new SaleListDetailDto(
            x.Id,
            x.SaleNumber,
            x.SaleDate,
            x.SaleSource,
            x.SaleStatus,
            x.Customer != null ? x.Customer.FullName : null,
            x.SalePerson.Name + " " + x.SalePerson.Surname,
            x.SaleItems.Count(),
            x.SaleItems.Sum(si => si.Quantity),
            x.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100m)),
            x.Payments.Select(p => p.PaymentMethod.Name).ToList()))
        .ToListAsync();

    return new SuccessDataResult<Pageable<SaleListDetailDto>>(
        new Pageable<SaleListDetailDto>(items, dto.PageIndex, dto.PageSize, total));
}
```

- [ ] **Step 9: SaleManager.CancelSaleAsync implement et**

```csharp
public async Task<IResult> CancelSaleAsync(Guid saleId, Guid cancelledByUserId)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var sale = await dbContext.Sales
        .Include(s => s.SaleItems)
        .FirstOrDefaultAsync(s => s.Id == saleId);

    if (sale is null)
        return new ErrorResult("Satış bulunamadı.");

    if (sale.SaleStatus == SaleStatus.Cancelled)
        return new ErrorResult("Bu satış zaten iptal edilmiş.");

    if (sale.SaleDate.Date != DateTimeOffset.UtcNow.Date)
        return new ErrorResult("Sadece bugünkü satışlar iptal edilebilir.");

    // Stok geri artır
    foreach (var item in sale.SaleItems)
    {
        await officeStockManager.IncreaseStockAtomicAsync(
            sale.BranchOfficeId, item.ProductVariantId, item.Quantity,
            StockMovementType.SaleCancellation, "Satış iptali");
    }

    sale.SaleStatus = SaleStatus.Cancelled;
    await dbContext.SaveChangesAsync();

    await applicationLogManager.AddLog(
        $"Satış iptal edildi: {sale.SaleNumber}", LogType.Sale, LogAction.Delete);

    return new SuccessResult("Satış iptal edildi.");
}
```

- [ ] **Step 10: Test çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleManagerMakeSaleTests"`
Expected: PASS

- [ ] **Step 11: Commit**

```bash
git add Application/Entegrasyon.Business/ Test/Entegrasyon.Test/Business/SaleManagerMakeSaleTests.cs
git commit -m "feat(business): SaleManager güncelleme — split payment, SaleNumber, detay, özet, iptal"
```

---

## Task 11: ISaleReturnManager + SaleReturnManager

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/ISaleReturnManager.cs`
- Create: `Application/Entegrasyon.Business/Concrete/SaleReturnManager.cs`
- Create: `Application/Entegrasyon.Business/Validation/Sale/CreateSaleReturnValidator.cs`
- Create: `Test/Entegrasyon.Test/Business/SaleReturnManagerTests.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/Business/SaleReturnManagerTests.cs
using Entegrasyon.Business.Concrete;
using Entegrasyon.Core.Utilities.Validation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Entegrasyon.Test.Business;

public class SaleReturnManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IApplicationLogManager> _logManagerMock = new();
    private readonly Mock<IFluentValidator> _validatorMock = new();
    private readonly Mock<IOfficeStockManager> _stockManagerMock = new();

    [Fact]
    public async Task CreateReturnAsync_PartialReturn_UpdatesReturnedQuantity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var saleId = Guid.NewGuid();
        var saleItemId = Guid.NewGuid();

        await using var setupContext = new IntegrationDbContext(options);
        setupContext.Sales.Add(new Sale
        {
            Id = saleId,
            SaleNumber = "S202604130001",
            SaleStatus = SaleStatus.Completed,
            SaleSource = SaleSource.POS,
            SaleDate = DateTimeOffset.UtcNow,
            BranchOfficeId = 1,
            SalePersonId = Guid.NewGuid(),
            SaleItems = [new SaleItem { Id = saleItemId, Quantity = 3, ReturnedQuantity = 0, UnitPrice = 100, TaxPercentage = 20, ProductVariantId = Guid.NewGuid() }]
        });
        await setupContext.SaveChangesAsync();

        _contextFactoryMock.Setup(x => x.CreateDbContextAsync(default))
            .ReturnsAsync(new IntegrationDbContext(options));

        _stockManagerMock.Setup(x => x.IncreaseStockAtomicAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<StockMovementType>(), It.IsAny<string>()))
            .ReturnsAsync(new SuccessResult());

        var manager = new SaleReturnManager(
            _contextFactoryMock.Object, _logManagerMock.Object,
            _validatorMock.Object, _stockManagerMock.Object);

        var dto = new CreateSaleReturnDto(
            SaleId: saleId,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Beden uymuyor",
            RefundPaymentMethodId: 1,
            Note: null,
            Items: [new SaleReturnItemDto(SaleItemId: saleItemId, Quantity: 1, Reason: "Küçük geldi")]);

        // Act
        var result = await manager.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeTrue();

        await using var verifyContext = new IntegrationDbContext(options);
        var saleItem = await verifyContext.SaleItems.FindAsync(saleItemId);
        saleItem!.ReturnedQuantity.Should().Be(1);

        var sale = await verifyContext.Sales.FindAsync(saleId);
        sale!.SaleStatus.Should().Be(SaleStatus.PartialReturn);
    }

    [Fact]
    public async Task CreateReturnAsync_ExceedsQuantity_ReturnsError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var saleId = Guid.NewGuid();
        var saleItemId = Guid.NewGuid();

        await using var setupContext = new IntegrationDbContext(options);
        setupContext.Sales.Add(new Sale
        {
            Id = saleId,
            SaleNumber = "S202604130002",
            SaleStatus = SaleStatus.Completed,
            SaleSource = SaleSource.POS,
            SaleDate = DateTimeOffset.UtcNow,
            BranchOfficeId = 1,
            SalePersonId = Guid.NewGuid(),
            SaleItems = [new SaleItem { Id = saleItemId, Quantity = 2, ReturnedQuantity = 1, UnitPrice = 50, TaxPercentage = 20, ProductVariantId = Guid.NewGuid() }]
        });
        await setupContext.SaveChangesAsync();

        _contextFactoryMock.Setup(x => x.CreateDbContextAsync(default))
            .ReturnsAsync(new IntegrationDbContext(options));

        var manager = new SaleReturnManager(
            _contextFactoryMock.Object, _logManagerMock.Object,
            _validatorMock.Object, _stockManagerMock.Object);

        var dto = new CreateSaleReturnDto(
            SaleId: saleId,
            ReturnedByUserId: Guid.NewGuid(),
            ReturnReason: "Hatalı ürün",
            RefundPaymentMethodId: 1,
            Note: null,
            Items: [new SaleReturnItemDto(SaleItemId: saleItemId, Quantity: 5, Reason: null)]);

        // Act
        var result = await manager.CreateReturnAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("iade edilebilir");
    }
}
```

- [ ] **Step 2: Test çalıştır — FAIL**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleReturnManagerTests"`
Expected: FAIL

- [ ] **Step 3: ISaleReturnManager interface oluştur**

```csharp
// Application/Entegrasyon.Business/Abstract/ISaleReturnManager.cs
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface ISaleReturnManager
{
    Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto);
    Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId);
    Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason);
    Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId);
}
```

- [ ] **Step 4: CreateSaleReturnValidator oluştur**

```csharp
// Application/Entegrasyon.Business/Validation/Sale/CreateSaleReturnValidator.cs
using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.Sale;

public class CreateSaleReturnValidator : AbstractValidator<CreateSaleReturnDto>
{
    public CreateSaleReturnValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.ReturnedByUserId).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().WithMessage("İade nedeni zorunludur.");
        RuleFor(x => x.Items)
            .NotNull()
            .Must(x => x.Count > 0).WithMessage("En az bir kalem seçmelisiniz.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SaleItemId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("İade adedi sıfırdan büyük olmalıdır.");
        });
    }
}
```

- [ ] **Step 5: SaleReturnManager implement et**

```csharp
// Application/Entegrasyon.Business/Concrete/SaleReturnManager.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Core.Utilities.Results;
using Entegrasyon.Core.Utilities.Validation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleReturnManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator,
    IOfficeStockManager officeStockManager) : ISaleReturnManager
{
    public async Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == dto.SaleId);

        if (sale is null)
            return new ErrorResult("Satış bulunamadı.");

        if (sale.SaleStatus == SaleStatus.Cancelled)
            return new ErrorResult("İptal edilmiş satış iade edilemez.");

        if (sale.SaleStatus == SaleStatus.FullReturn)
            return new ErrorResult("Bu satışın tamamı zaten iade edilmiş.");

        // Miktar kontrolü
        decimal totalRefund = 0;
        var returnItems = new List<SaleReturnItem>();

        foreach (var itemDto in dto.Items)
        {
            var saleItem = sale.SaleItems.FirstOrDefault(si => si.Id == itemDto.SaleItemId);
            if (saleItem is null)
                return new ErrorResult($"Satış kalemi bulunamadı: {itemDto.SaleItemId}");

            var availableQty = saleItem.Quantity - saleItem.ReturnedQuantity;
            if (itemDto.Quantity > availableQty)
                return new ErrorResult(
                    $"'{saleItem.ProductTitle}' için maksimum {availableQty} adet iade edilebilir.");

            // İade tutarı hesapla (KDV dahil)
            var unitPriceWithVat = saleItem.UnitPrice * (1 + (decimal)saleItem.TaxPercentage / 100m);
            totalRefund += Math.Round(unitPriceWithVat * itemDto.Quantity, 2);

            returnItems.Add(new SaleReturnItem
            {
                SaleItemId = itemDto.SaleItemId,
                Quantity = itemDto.Quantity,
                Reason = itemDto.Reason
            });

            // ReturnedQuantity güncelle
            saleItem.ReturnedQuantity += itemDto.Quantity;
        }

        var saleReturn = new SaleReturn
        {
            SaleId = dto.SaleId,
            ReturnDate = DateTimeOffset.UtcNow,
            ReturnedByUserId = dto.ReturnedByUserId,
            ReturnStatus = ReturnStatus.Pending,
            ReturnReason = dto.ReturnReason,
            RefundPaymentMethodId = dto.RefundPaymentMethodId,
            RefundAmount = totalRefund,
            Note = dto.Note,
            Items = returnItems
        };

        // Sale durumunu güncelle
        var allItemsFullyReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allItemsFullyReturned ? SaleStatus.FullReturn : SaleStatus.PartialReturn;

        dbContext.SaleReturns.Add(saleReturn);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade talebi oluşturuldu: {sale.SaleNumber}, Tutar: {totalRefund:C}", LogType.Sale, LogAction.Add);

        return new SuccessResult("İade talebi oluşturuldu.");
    }

    public async Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Sale)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (saleReturn is null)
            return new ErrorResult("İade kaydı bulunamadı.");

        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Bu iade zaten işlenmiş.");

        saleReturn.ReturnStatus = ReturnStatus.Approved;
        saleReturn.ApprovedByUserId = approvedByUserId;

        // Stok geri artır
        foreach (var item in saleReturn.Items)
        {
            await officeStockManager.IncreaseStockAtomicAsync(
                saleReturn.Sale.BranchOfficeId,
                item.SaleItem.ProductVariantId,
                item.Quantity,
                StockMovementType.Return,
                $"İade onayı: {saleReturn.Sale.SaleNumber}");
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade onaylandı: {saleReturn.Sale.SaleNumber}", LogType.Sale, LogAction.Update);

        return new SuccessResult("İade onaylandı, stok güncellendi.");
    }

    public async Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Sale).ThenInclude(s => s.SaleItems)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (saleReturn is null)
            return new ErrorResult("İade kaydı bulunamadı.");

        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Bu iade zaten işlenmiş.");

        saleReturn.ReturnStatus = ReturnStatus.Rejected;
        saleReturn.Note = (saleReturn.Note ?? "") + $" [Ret nedeni: {reason}]";

        // ReturnedQuantity geri al
        foreach (var item in saleReturn.Items)
            item.SaleItem.ReturnedQuantity -= item.Quantity;

        // Sale durumunu yeniden hesapla
        var sale = saleReturn.Sale;
        var hasAnyReturn = sale.SaleItems.Any(si => si.ReturnedQuantity > 0);
        var allReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allReturned ? SaleStatus.FullReturn
            : hasAnyReturn ? SaleStatus.PartialReturn
            : SaleStatus.Completed;

        await dbContext.SaveChangesAsync();
        return new SuccessResult("İade reddedildi.");
    }

    public async Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.ReturnedBy)
            .Include(r => r.ApprovedBy)
            .Include(r => r.RefundPaymentMethod)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        return saleReturn is null
            ? new ErrorDataResult<SaleReturn>("İade kaydı bulunamadı.")
            : new SuccessDataResult<SaleReturn>(saleReturn);
    }
}
```

- [ ] **Step 6: DI kaydı ekle**

`ApplicationDependencyExtension.cs`'e:
```csharp
services.AddScoped<ISaleReturnManager, SaleReturnManager>();
```

- [ ] **Step 7: Test çalıştır — PASS**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~SaleReturnManagerTests"`
Expected: 2 tests passed

- [ ] **Step 8: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ISaleReturnManager.cs Application/Entegrasyon.Business/Concrete/SaleReturnManager.cs Application/Entegrasyon.Business/Validation/Sale/CreateSaleReturnValidator.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs Test/Entegrasyon.Test/Business/SaleReturnManagerTests.cs
git commit -m "feat(business): ISaleReturnManager — kısmi iade, onay/red akışı, stok geri artışı"
```

---

## Task 12: POS X/Z Raporu Business Layer

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IPOSSessionManager.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/POS/POSSessionManager.cs`
- Create: `Test/Entegrasyon.Test/Business/POSReportTests.cs`

- [ ] **Step 1: Failing test yaz**

```csharp
// Test/Entegrasyon.Test/Business/POSReportTests.cs
// X raporu: Açık oturumdaki satışların ödeme yöntemi ve KDV dökümünü döner
// Test setup: POSSession + POSTransaction + Sale + SaleItem + SalePayment
// Assert: TransactionCount, TotalSales, PaymentBreakdown, VatBreakdown doğruluğu
```

Not: Tam test kodu implementation sırasında yazılacak — setup karmaşık çünkü çok sayıda ilişkili entity gerekiyor. Temel test senaryoları:
- `GetXReportAsync_ReturnsCorrectPaymentBreakdown`
- `GetXReportAsync_ReturnsCorrectVatBreakdown`
- `GetZReportAsync_IncludesActualCashAndDifference`

- [ ] **Step 2: IPOSSessionManager'a X/Z rapor metodları ekle**

```csharp
// Mevcut interface'e ekle:
Task<IDataResult<POSReportDto>> GetXReportAsync(long sessionId);
Task<IDataResult<POSReportDto>> GetZReportAsync(long sessionId);
```

- [ ] **Step 3: POSSessionManager'a implement et**

```csharp
public async Task<IDataResult<POSReportDto>> GetXReportAsync(long sessionId)
{
    return await BuildReportAsync(sessionId, isZReport: false);
}

public async Task<IDataResult<POSReportDto>> GetZReportAsync(long sessionId)
{
    return await BuildReportAsync(sessionId, isZReport: true);
}

private async Task<IDataResult<POSReportDto>> BuildReportAsync(long sessionId, bool isZReport)
{
    await using var dbContext = await contextFactory.CreateDbContextAsync();

    var session = await dbContext.POSSessions
        .Include(s => s.Cashier)
        .Include(s => s.CashMovements)
        .FirstOrDefaultAsync(s => s.Id == sessionId);

    if (session is null)
        return new ErrorDataResult<POSReportDto>("Kasa oturumu bulunamadı.");

    // Oturumdaki tüm satışları al
    var saleIds = await dbContext.POSTransactions
        .Where(t => t.POSSessionId == sessionId)
        .Select(t => t.SaleId)
        .ToListAsync();

    var sales = await dbContext.Sales
        .Include(s => s.SaleItems)
        .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
        .Where(s => saleIds.Contains(s.Id) && s.SaleStatus != SaleStatus.Cancelled)
        .ToListAsync();

    // Ödeme yöntemi dağılımı
    var paymentBreakdown = sales
        .SelectMany(s => s.Payments)
        .GroupBy(p => p.PaymentMethod.Name)
        .Select(g => new PaymentMethodSummaryDto(g.Key, g.Count(), g.Sum(p => p.Amount)))
        .ToList();

    // KDV dökümü
    var vatBreakdown = sales
        .SelectMany(s => s.SaleItems)
        .GroupBy(si => (decimal)si.TaxPercentage)
        .Select(g =>
        {
            var taxBase = g.Sum(si => si.UnitPrice * si.Quantity);
            var vatAmount = taxBase * g.Key / 100m;
            return new VatSummaryLineDto(g.Key, Math.Round(taxBase, 2), Math.Round(vatAmount, 2), Math.Round(taxBase + vatAmount, 2));
        })
        .OrderBy(v => v.VatRate)
        .ToList();

    var totalSales = sales.Sum(s => s.Payments.Sum(p => p.Amount));
    var totalCash = sales.SelectMany(s => s.Payments)
        .Where(p => p.PaymentMethod.SystemCode == "Cash")
        .Sum(p => p.Amount);

    var cashMovementsNet = session.CashMovements
        .Sum(cm => cm.MovementType == CashMovementType.CashIn ? cm.Amount : -cm.Amount);

    // İade tutarları
    var totalReturns = await dbContext.SaleReturns
        .Where(r => saleIds.Contains(r.SaleId) && r.ReturnStatus == ReturnStatus.Approved)
        .SumAsync(r => r.RefundAmount);

    var expectedCash = session.OpeningCash + totalCash + cashMovementsNet;

    var report = new POSReportDto
    {
        SessionId = sessionId,
        CashierName = $"{session.Cashier.Name} {session.Cashier.Surname}",
        OpenedAt = session.OpenedAt,
        ClosedAt = session.ClosedAt,
        OpeningCash = session.OpeningCash,
        TransactionCount = sales.Count,
        TotalSales = totalSales,
        TotalReturns = totalReturns,
        NetSales = totalSales - totalReturns,
        PaymentBreakdown = paymentBreakdown,
        VatBreakdown = vatBreakdown,
        ExpectedCash = expectedCash,
        ActualCash = isZReport ? session.ClosingCash : null,
        CashDifference = isZReport && session.ClosingCash.HasValue
            ? session.ClosingCash.Value - expectedCash
            : null
    };

    return new SuccessDataResult<POSReportDto>(report);
}
```

- [ ] **Step 4: Test çalıştır, build doğrula**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPOSSessionManager.cs Application/Entegrasyon.Business/Concrete/POS/POSSessionManager.cs Test/Entegrasyon.Test/Business/POSReportTests.cs
git commit -m "feat(business): POS X/Z raporu — ödeme dağılımı, KDV dökümü, kasa mutabakat"
```

---

## Task 13: Ödeme Yöntemi Ayarları Sayfası (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Settings/SettingsController.cs`
- Create: `Application/Entegrasyon.MVC/Features/Settings/Views/PaymentMethods.cshtml`

- [ ] **Step 1: SettingsController'a endpoint'ler ekle**

```csharp
[HttpGet("/settings/payment-methods")]
public async Task<IActionResult> PaymentMethods()
{
    var tenantId = User.GetTenantId();
    var result = await paymentMethodManager.GetAllPaymentMethodsAsync(tenantId);
    ViewData.SetPageTitle("Ödeme Yöntemleri");
    ViewData.SetActiveNav("settings");
    return View(result.Data);
}

[HttpPost("/settings/payment-methods/{id:int}/toggle")]
public async Task<IActionResult> TogglePaymentMethod(int id)
{
    var result = await paymentMethodManager.TogglePaymentMethodAsync(id);
    if (Request.IsHtmx())
    {
        var tenantId = User.GetTenantId();
        var methods = await paymentMethodManager.GetAllPaymentMethodsAsync(tenantId);
        return PartialView("PaymentMethods", methods.Data);
    }
    TempData.SetSuccess(result.Message!);
    return RedirectToAction(nameof(PaymentMethods));
}

[HttpPost("/settings/payment-methods/{id:int}/update")]
public async Task<IActionResult> UpdatePaymentMethod(int id, string name, string icon, decimal? commissionRate)
{
    var result = await paymentMethodManager.UpdatePaymentMethodAsync(id, name, icon, commissionRate);
    TempData.SetSuccess(result.Message!);
    return RedirectToAction(nameof(PaymentMethods));
}

[HttpPost("/settings/payment-methods/reorder")]
public async Task<IActionResult> ReorderPaymentMethods([FromBody] List<int> orderedIds)
{
    var result = await paymentMethodManager.ReorderPaymentMethodsAsync(orderedIds);
    return result.Success ? Ok() : BadRequest(result.Message);
}
```

- [ ] **Step 2: PaymentMethods.cshtml oluştur**

Tabler UI ile sıralanabilir tablo: her satırda ikon, ad, sistem kodu, aktif/pasif toggle, komisyon, düzenle butonu. SortableJS ile drag-and-drop sıralama. HTMX ile toggle. Mevcut Settings sayfasındaki tab yapısına uyumlu.

Not: Tam HTML, mevcut Settings view'larının pattern'ine bakılarak implementation sırasında yazılacak. Tabler card + list-group yapısı kullanılacak.

- [ ] **Step 3: DI — IPaymentMethodManager SettingsController'a inject et**

Constructor'a ekle: `IPaymentMethodManager paymentMethodManager`

- [ ] **Step 4: Build + test**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Settings/
git commit -m "feat(mvc): ödeme yöntemi ayarları sayfası — toggle, sıralama, düzenleme"
```

---

## Task 14: Satış Listesi Sayfası Yeniden Tasarım (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Index.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleTable.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleSummaryCards.cshtml`

- [ ] **Step 1: SaleController.Index güncelle**

Mevcut Index action'ını güncelle — `SaleSummaryDto` ve güncellenmiş `SalePageableDto` kullan:

```csharp
[HttpGet("/sales")]
public async Task<IActionResult> Index(
    string? search, DateTimeOffset? startDate, DateTimeOffset? endDate,
    SaleSource? source, SaleStatus? status, int page = 0)
{
    var dto = new SalePageableDto(
        CustomerId: null,
        DateBetweenStart: startDate ?? DateTimeOffset.UtcNow.Date,
        DateBetweenEnd: endDate ?? DateTimeOffset.UtcNow.Date.AddDays(1),
        SalePersonId: Guid.Empty,
        SaleSource: source,
        SaleStatus: status,
        FullTextSearchKey: search ?? "",
        PageIndex: page);

    var salesResult = await saleManager.GetSalesPageable(dto);
    var summaryResult = await saleManager.GetSalesSummaryAsync(dto);

    ViewData.SetPageTitle("Satışlar");
    ViewData.SetActiveNav("sales");
    ViewBag.Summary = summaryResult.Data;
    ViewBag.CurrentFilters = dto;

    if (Request.IsHtmx())
        return PartialView("Partials/_SaleTable", salesResult.Data);

    return View(salesResult.Data);
}
```

- [ ] **Step 2: _SaleSummaryCards.cshtml oluştur**

4 Tabler stat card: Toplam Satış, Satış Adedi, Ortalama Sepet, İade Tutarı. `@model SaleSummaryDto`

- [ ] **Step 3: Index.cshtml yeniden yaz**

Üstte `_SaleSummaryCards` partial, altında filtre alanı (tarih, kaynak, durum, arama), altında `_SaleTable` partial.

- [ ] **Step 4: _SaleTable.cshtml yeniden yaz**

Yeni sütunlar: Fiş No, Tarih, Kaynak badge, Müşteri, Kasiyer, Ürün Adedi, Tutar (KDV dahil), Ödeme yöntemleri badge, Durum badge. `<tr data-href="/sales/@item.Id">` ile tıklanabilir satır.

- [ ] **Step 5: Build + test**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(mvc): satış listesi yeniden tasarım — özet kartlar, gelişmiş filtreler, KDV dahil"
```

---

## Task 15: Satış Detay Sayfası (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Detail.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Print.cshtml`

- [ ] **Step 1: SaleController'a Detail + Print endpoint'leri ekle**

```csharp
[HttpGet("/sales/{id:guid}")]
public async Task<IActionResult> Detail(Guid id)
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success)
    {
        TempData.SetError(result.Message!);
        return RedirectToAction(nameof(Index));
    }

    var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(User.GetTenantId());

    ViewData.SetPageTitle($"Satış Detay — {result.Data!.SaleNumber}");
    ViewData.SetActiveNav("sales");
    ViewData.SetBreadcrumb([("Satışlar", "/sales"), (result.Data.SaleNumber, null)]);
    ViewBag.PaymentMethods = paymentMethods.Data;

    return View(result.Data);
}

[HttpGet("/sales/{id:guid}/print")]
public async Task<IActionResult> Print(Guid id)
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success) return NotFound();
    return View(result.Data);
}

[HttpPost("/sales/{id:guid}/cancel")]
public async Task<IActionResult> Cancel(Guid id)
{
    var userId = User.GetUserId();
    var result = await saleManager.CancelSaleAsync(id, userId);
    TempData.SetResult(result);
    return RedirectToAction(nameof(Detail), new { id });
}
```

- [ ] **Step 2: Detail.cshtml oluştur**

Tabler card layout:
- Üst card: Satış bilgileri (Fiş No, Tarih, Kaynak, Kasiyer, Şube, Müşteri, Durum)
- Kalemler tablosu: Ürün, Adet, Birim Fiyat (KDV dahil), KDV Oranı, Toplam, İade Durumu
- Ödeme bilgileri card: SalePayment listesi
- KDV dökümü card: Oran bazlı tablo
- İade geçmişi card: SaleReturn listesi (varsa)
- Aksiyon butonları: İade Et, Yazdır, İptal Et

`@model SaleDetailDto`

- [ ] **Step 3: Print.cshtml oluştur**

Yazdırılabilir fiş layout: minimal CSS, `@media print` kuralları, şirket bilgisi, kalemler, KDV dökümü, ödeme bilgisi, fiş no + tarih.

- [ ] **Step 4: Build + test**

Run: `dotnet build Entegrasyon.sln`

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(mvc): satış detay sayfası — kalemler, ödemeler, KDV dökümü, yazdırma"
```

---

## Task 16: İade Akışı UI (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Sales/SaleController.cs`
- Create: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleReturnDialog.cshtml`

- [ ] **Step 1: SaleController'a iade endpoint'leri ekle**

```csharp
[HttpGet("/sales/{id:guid}/return-dialog")]
public async Task<IActionResult> ReturnDialog(Guid id)
{
    var result = await saleManager.GetSaleDetailAsync(id);
    if (!result.Success) return BadRequest(result.Message);

    var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(User.GetTenantId());
    ViewBag.PaymentMethods = paymentMethods.Data;

    return PartialView("Partials/_SaleReturnDialog", result.Data);
}

[HttpPost("/sales/{saleId:guid}/return")]
public async Task<IActionResult> CreateReturn(Guid saleId, CreateSaleReturnDto dto)
{
    var result = await saleReturnManager.CreateReturnAsync(dto with
    {
        SaleId = saleId,
        ReturnedByUserId = User.GetUserId()
    });
    TempData.SetResult(result);
    return RedirectToAction(nameof(Detail), new { id = saleId });
}

[HttpPost("/sales/returns/{returnId:long}/approve")]
public async Task<IActionResult> ApproveReturn(long returnId, Guid saleId)
{
    var result = await saleReturnManager.ApproveReturnAsync(returnId, User.GetUserId());
    TempData.SetResult(result);
    return RedirectToAction(nameof(Detail), new { id = saleId });
}

[HttpPost("/sales/returns/{returnId:long}/reject")]
public async Task<IActionResult> RejectReturn(long returnId, Guid saleId, string reason)
{
    var result = await saleReturnManager.RejectReturnAsync(returnId, User.GetUserId(), reason);
    TempData.SetResult(result);
    return RedirectToAction(nameof(Detail), new { id = saleId });
}
```

- [ ] **Step 2: _SaleReturnDialog.cshtml oluştur**

Modal dialog: Kalem seçimi (checkbox + adet), iade nedeni (textarea), ödeme yöntemi seçimi, otomatik tutar hesaplama (JS). `hx-get="/sales/{id}/return-dialog"` ile HTMX modal açma.

- [ ] **Step 3: Detail.cshtml'e iade butonları ekle**

"İade Et" butonu → HTMX modal. İade geçmişinde Pending iade varsa Onayla/Reddet butonları.

- [ ] **Step 4: Build + test**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(mvc): iade akışı UI — modal dialog, onay/red butonları"
```

---

## Task 17: POS Ödeme Dialog Yeniden Tasarım (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs`
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSPaymentDialog.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs`

- [ ] **Step 1: POSController.PaymentDialog güncelle**

Aktif ödeme yöntemlerini DB'den yükle ve dialog'a gönder:

```csharp
[HttpGet("/pos/payment-dialog")]
public async Task<IActionResult> PaymentDialog()
{
    var cart = GetCartFromSession();
    if (cart.Items.Count == 0) return BadRequest("Sepet boş.");

    var tenantId = User.GetTenantId();
    var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(tenantId);

    var vm = new POSPaymentDialogVm
    {
        SessionId = GetActiveSessionId(),
        Subtotal = cart.Subtotal,
        VatTotal = cart.VatTotal,
        GrandTotal = cart.GrandTotal,
        ItemCount = cart.TotalItems,
        PaymentMethods = paymentMethods.Data ?? []
    };

    return PartialView("Partials/_POSPaymentDialog", vm);
}
```

- [ ] **Step 2: POSPaymentDialogVm güncelle**

```csharp
// POSTerminalVm.cs'e ekle veya güncelle:
public class POSPaymentDialogVm
{
    public long SessionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public int ItemCount { get; set; }
    public List<PaymentMethodDefinition> PaymentMethods { get; set; } = [];
}
```

- [ ] **Step 3: _POSPaymentDialog.cshtml yeniden yaz**

Hardcoded 3-tab yerine dinamik ödeme yöntemleri:
- Her aktif yöntem bir kart/buton olarak gösterilir
- "Ödeme Ekle" ile split payment — birden fazla ödeme kaydı eklenebilir
- Kalan tutar otomatik hesaplama (JS)
- RequiresCashInput → nakit girişi + para üstü
- RequiresAuthCode → provizyon kodu inputu
- Toplam >= GrandTotal → "Tamamla" butonu aktif

- [ ] **Step 4: POSController.CompleteSale güncelle**

Mevcut `complete-sale` endpoint'ini güncelle — `MakeSaleDto.Payments` listesini POST data'dan al:

```csharp
[HttpPost("/pos/complete-sale")]
public async Task<IActionResult> CompleteSale(List<SalePaymentDto> payments)
{
    var cart = GetCartFromSession();
    var sessionId = GetActiveSessionId();

    var dto = new MakeSaleDto(
        SalePersonId: User.GetUserId(),
        CustomerId: GetCustomerIdFromSession(),
        GeneralDiscount: 0,
        BranchOfficeId: User.GetBranchOfficeId(),
        SaleSource: SaleSource.POS,
        Note: null,
        SaleItems: cart.Items.Select(i => new SaleItemDto(
            i.VariantId, (double)i.VatRate, 0, i.UnitPrice, i.Quantity, "")).ToList(),
        Payments: payments);

    var result = await saleManager.MakeSale(dto);
    if (!result.Success)
    {
        TempData.SetError(result.Message!);
        return RedirectToAction(nameof(Index));
    }

    // POS Transaction kayıt
    var totalCashReceived = payments.Where(p => p.CashReceived.HasValue).Sum(p => p.CashReceived!.Value);
    var totalChangeGiven = payments.Where(p => p.CashReceived.HasValue).Sum(p => Math.Max(0, p.CashReceived!.Value - p.Amount));

    await posSessionManager.RecordTransactionAsync(new POSTransactionDto
    {
        POSSessionId = sessionId,
        SaleId = /* son eklenen sale ID */,
        CashReceived = totalCashReceived,
        ChangeGiven = totalChangeGiven,
        TransactionAt = DateTimeOffset.UtcNow
    });

    ClearCartSession();
    TempData.SetSuccess("Satış tamamlandı.");
    return RedirectToAction(nameof(Index));
}
```

- [ ] **Step 5: Build + test**

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/
git commit -m "feat(mvc): POS ödeme dialog yeniden tasarım — dinamik ödeme yöntemleri, split payment"
```

---

## Task 18: POS KDV Dahil + Müşteri Seçimi

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Index.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/POS/Views/Partials/_POSCart.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/POS/ViewModels/POSTerminalVm.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/ViewModels/SaleEntryVm.cs`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Create.cshtml`
- Modify: `Application/Entegrasyon.MVC/Features/Sales/Views/Partials/_SaleCart.cshtml`

- [ ] **Step 1: ViewModel'lara KDV dahil computed property ekle**

```csharp
// SaleEntryVm.cs — SaleCartItemVm'e:
public decimal UnitPriceWithVat => Math.Round(UnitPrice * (1 + VatRate / 100m), 2);
public decimal LineTotalWithVat => Math.Round(UnitPrice * Quantity * (1 + VatRate / 100m), 2);

// POSTerminalVm.cs — POSCartItemVm'e:
public decimal UnitPriceWithVat => Math.Round(UnitPrice * (1 + VatRate / 100m), 2);
public decimal LineTotalWithVat => Math.Round(UnitPrice * Quantity * (1 + VatRate / 100m), 2);
```

- [ ] **Step 2: _POSCart.cshtml ve _SaleCart.cshtml güncelle**

Fiyat gösterimlerini `UnitPriceWithVat` ve `LineTotalWithVat` ile değiştir. Footer'da GrandTotal = toplam KDV dahil tutar.

- [ ] **Step 3: POS Index.cshtml'e müşteri seçimi ekle**

Sales modülündeki `_SaleCustomerBadge` ve `_SaleCustomerResults` partial'larını POS'a da ekle. HTMX endpoint'leri POS controller'a eklenmeli.

- [ ] **Step 4: Build + test**

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/ Application/Entegrasyon.MVC/Features/Sales/
git commit -m "feat(mvc): KDV dahil fiyat gösterimi + POS müşteri seçimi"
```

---

## Task 19: POS X/Z Rapor Sayfaları (MVC)

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/POS/POSController.cs`
- Create: `Application/Entegrasyon.MVC/Features/POS/Views/XReport.cshtml`
- Create: `Application/Entegrasyon.MVC/Features/POS/Views/ZReport.cshtml`

- [ ] **Step 1: POSController'a rapor endpoint'leri ekle**

```csharp
[HttpGet("/pos/x-report")]
public async Task<IActionResult> XReport()
{
    var sessionId = GetActiveSessionId();
    var result = await posSessionManager.GetXReportAsync(sessionId);
    if (!result.Success)
    {
        TempData.SetError(result.Message!);
        return RedirectToAction(nameof(Index));
    }
    ViewData.SetPageTitle("X Raporu");
    return View(result.Data);
}

[HttpGet("/pos/z-report/{sessionId:long}")]
public async Task<IActionResult> ZReport(long sessionId)
{
    var result = await posSessionManager.GetZReportAsync(sessionId);
    if (!result.Success)
    {
        TempData.SetError(result.Message!);
        return RedirectToAction(nameof(Index));
    }
    ViewData.SetPageTitle("Z Raporu");
    return View(result.Data);
}
```

- [ ] **Step 2: XReport.cshtml oluştur**

Yazdırılabilir rapor sayfası (`@media print`):
- Kasiyer, kasa, açılış saati, başlangıç kasası
- İşlem sayısı, toplam satış, toplam iade, net satış
- Ödeme yöntemi bazlı tablo (ad, adet, tutar)
- KDV dökümü tablosu (oran, matrah, KDV, toplam)
- Beklenen kasa tutarı
- "Yazdır" butonu

- [ ] **Step 3: ZReport.cshtml oluştur**

X raporu + ekstra bilgiler:
- Kapanış saati, gerçek kasa tutarı, fark (renk kodlu alert)
- "Yazdır" butonu

- [ ] **Step 4: POS Index'e X raporu ve kasa kapama sonrası Z raporu linki ekle**

Kasa açıkken "X Raporu" butonu. Kasa kapama sonrası Z raporuna yönlendir.

- [ ] **Step 5: Build + test**

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/POS/
git commit -m "feat(mvc): POS X/Z rapor sayfaları — yazdırılabilir, ödeme ve KDV dökümü"
```

---

## Task 20: Tüm Testleri Çalıştır ve Final Doğrulama

**Files:** Tüm test projeleri

- [ ] **Step 1: Build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded, 0 warnings (veya sadece mevcut uyarılar)

- [ ] **Step 2: Unit testler**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Tüm testler geçer

- [ ] **Step 3: Integration testler**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: Tüm testler geçer (Docker çalışıyor olmalı)

- [ ] **Step 4: MVC testler**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj`
Expected: Tüm testler geçer

- [ ] **Step 5: Manual test**

Uygulamayı çalıştır ve şunları test et:
1. Ayarlar → Ödeme Yöntemleri: Toggle, sıralama, düzenleme
2. POS: Kasa aç → ürün ekle → ödeme (split payment) → kasa kapat
3. POS: X raporu, Z raporu
4. Satışlar: Liste (özet kartlar, filtreler), detay
5. Satış detay: İade başlat → onayla
6. Tüm fiyatlar KDV dahil gösteriliyor mu?

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "test: satış modülü final doğrulama — tüm testler geçiyor"
```

using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Müşteri Raporu iş katmanı — RFM segmentasyon, cohort retention ve dormant geri kazanım kuponu.
/// Read yolları salt-okuma (user-facing log yalnızca hata; ILogger her zaman).
/// <see cref="SendRecoveryCouponAsync"/> mutasyon → tam 3-adım pipeline + çift log.
/// </summary>
public class CustomerReportManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator fluentValidator,
    IRandomGenerator randomGenerator,
    IApplicationLogManager applicationLogManager,
    ILogger<CustomerReportManager> logger) : ICustomerReportManager
{
    private const int RecentDays = 30;
    private const int DormantDays = 90;
    private const int CodeLength = 6;

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetRfmPageableAsync(
        string? segment, int pageIndex = 0, int itemCount = 50, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        var recentCutoff = DateTimeOffset.UtcNow.AddDays(-RecentDays);
        var dormantCutoff = DateTimeOffset.UtcNow.AddDays(-DormantDays);

        // Recency (MAX OrderDate) + monetary (SUM GrossAmount) tek server-side GroupBy(CustomerId).
        // Index: IX_Orders_CustomerId_OrderDate (partial: CustomerId NOT NULL AND NOT IsDeleted).
        // NOT: Order.GrossAmount Postgres'te `money` tipinde. SQL-tarafı SUM(money) + EF'in eklediği
        // COALESCE(..., numeric) "could not convert type money to numeric" verir. Bu yüzden agregat
        // SQL'de DEĞİL: yalnızca (CustomerId, OrderDate, GrossAmount) projeksiyonu çekilip GroupBy +
        // MAX/SUM bellekte yapılır. GrossAmount'ı `(decimal?)` cast'i numeric'e çevirir → güvenli.
        var orderRows = await db.Orders
            .Where(o => o.CustomerId != null && o.OrderDate != null)
            .Select(o => new
            {
                CustomerId = o.CustomerId!.Value,
                o.OrderDate,
                Gross = (decimal?)o.GrossAmount
            })
            .ToListAsync(ct);

        var perCustomer = orderRows
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LastPurchase = g.Max(o => o.OrderDate),
                TotalSpend = g.Sum(o => o.Gross ?? 0m)
            })
            .ToList();

        // VIP monetary eşiği — üst %20 persentil. Harcaması olan müşteriler arasında server-side
        // sıralı; bellekte sadece skaler eşik hesaplanır (full müşteri-load yok, agregat zaten daraltıldı).
        var spends = perCustomer.Select(x => x.TotalSpend).Where(s => s > 0m).OrderBy(s => s).ToList();
        decimal vipThreshold = spends.Count > 0
            ? spends[(int)Math.Floor(spends.Count * 0.8)]  // 80. persentil → üst %20
            : decimal.MaxValue;

        var spendMap = perCustomer.ToDictionary(x => x.CustomerId);

        // Segmenti server-side filtrelemek için ilgili müşteri-id kümesini önce çıkar.
        // (Müşteri listesi büyük olabilir → segment filtresi sorguya HashSet.Contains ile taşınır.)
        string? seg = string.IsNullOrWhiteSpace(segment) ? null : segment.Trim().ToLowerInvariant();

        var query = db.Customers.AsQueryable();

        if (seg is "vip" or "risk" or "dormant")
        {
            // Bu segmentler sipariş geçmişine dayanır → uygun müşteri-id kümesi.
            var ids = perCustomer
                .Where(x => SegmentMatches(seg, x.LastPurchase, x.TotalSpend, recentCutoff, dormantCutoff, vipThreshold))
                .Select(x => x.CustomerId)
                .ToHashSet();
            query = query.Where(c => ids.Contains(c.Id));
        }
        else if (seg == "yeni")
        {
            query = query.Where(c => c.CreatedAt >= recentCutoff);
        }

        int total = await query.CountAsync(ct);

        var customers = await query
            .OrderBy(c => c.FullName)
            .Skip(pageIndex * itemCount)
            .Take(itemCount)
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                c.CustomerType,
                c.FullName,
                c.PhoneNumber,
                Address = c.Address != null ? c.Address.FullAddress : null,
                CorporateName = (c as CorporateCustomer)!.CorporateName,
                NationalId = (c as RetailCustomer)!.NationalIdentity,
                TaxNumber = (c as CorporateCustomer)!.TaxNumber,
                c.IsActive,
                c.DeactivatedAt,
                c.DeactivationReason
            })
            .ToListAsync(ct);

        var items = customers.Select(c =>
        {
            spendMap.TryGetValue(c.Id, out var agg);
            DateOnly? last = agg?.LastPurchase is { } lp ? DateOnly.FromDateTime(lp.UtcDateTime) : null;
            decimal? spend = agg is not null ? agg.TotalSpend : null;

            var rfm = ResolveSegment(last, spend, c.CreatedAt, recentCutoff, dormantCutoff, vipThreshold);

            return new CustomerDetailDto(
                c.CreatedAt, c.Id,
                c.NationalId ?? c.TaxNumber ?? "",
                c.FullName ?? "", c.CorporateName ?? "",
                0, c.PhoneNumber ?? "", c.Address ?? "",
                c.CustomerType ?? "", c.IsActive, c.DeactivatedAt, c.DeactivationReason)
            {
                LastPurchaseDate = last,
                TotalSpend = spend,
                RfmSegment = rfm
            };
        }).ToList();

        logger.LogInformation(
            "RFM müşteri raporu hesaplandı. Segment={Segment} Toplam={Total} VipEşik={Threshold}",
            seg ?? "tümü", total, vipThreshold == decimal.MaxValue ? 0 : vipThreshold);

        return new SuccessDataResult<Pageable<CustomerDetailDto>>(
            new Pageable<CustomerDetailDto>(items, pageIndex, itemCount, total));
    }

    /// <summary>
    /// Tek müşterinin RFM segment etiketini döner. Öncelik: VIP &gt; Risk/Dormant &gt; Yeni &gt; Standart.
    /// </summary>
    private static string ResolveSegment(
        DateOnly? lastPurchase, decimal? totalSpend, DateTimeOffset createdAt,
        DateTimeOffset recentCutoff, DateTimeOffset dormantCutoff, decimal vipThreshold)
    {
        var hasOrders = lastPurchase.HasValue;
        var recent = hasOrders && lastPurchase!.Value >= DateOnly.FromDateTime(recentCutoff.UtcDateTime);
        var dormant = hasOrders && lastPurchase!.Value <= DateOnly.FromDateTime(dormantCutoff.UtcDateTime);

        if (recent && (totalSpend ?? 0m) >= vipThreshold && vipThreshold != decimal.MaxValue)
            return "VIP";
        if (dormant)
            return "Dormant";
        if (createdAt >= recentCutoff)
            return "Yeni";
        return "Standart";
    }

    /// <summary>Segment filtresi için aggregate satırının verilen segmente uyup uymadığı.</summary>
    private static bool SegmentMatches(
        string seg, DateTimeOffset? lastPurchase, decimal totalSpend,
        DateTimeOffset recentCutoff, DateTimeOffset dormantCutoff, decimal vipThreshold)
    {
        var last = lastPurchase is { } lp ? DateOnly.FromDateTime(lp.UtcDateTime) : (DateOnly?)null;
        var recent = last.HasValue && last.Value >= DateOnly.FromDateTime(recentCutoff.UtcDateTime);
        var dormant = last.HasValue && last.Value <= DateOnly.FromDateTime(dormantCutoff.UtcDateTime);

        return seg switch
        {
            "vip" => recent && totalSpend >= vipThreshold && vipThreshold != decimal.MaxValue,
            "risk" or "dormant" => dormant,
            _ => false
        };
    }

    public async Task<IReadOnlyList<CustomerCohortRowDto>> GetCohortRetentionAsync(
        int maxMonths = 12, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // Müşteri başına (ay-bucket'lı) tekil alışveriş ayları. Sorguda yalnızca CustomerId+OrderDate
        // çekilir; aylık bucket + cohort pivotu bellekte (müşteri-ay sayısı küçük, sınır maxMonths).
        var raw = await db.Orders
            .Where(o => o.CustomerId != null && o.OrderDate != null)
            .Select(o => new { CustomerId = o.CustomerId!.Value, o.OrderDate })
            .ToListAsync(ct);

        if (raw.Count == 0)
            return [];

        // Müşteri → benzersiz alışveriş ayları (yyyy-MM tabanlı ay-indeksi).
        var byCustomer = raw
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => MonthIndex(x.OrderDate!.Value)).Distinct().OrderBy(m => m).ToList());

        // Her müşterinin kohortu = ilk alışveriş ayı.
        var cohorts = byCustomer
            .GroupBy(kv => kv.Value[0])
            .OrderByDescending(g => g.Key)
            .Take(maxMonths)
            .ToList();

        var rows = new List<CustomerCohortRowDto>();
        foreach (var cohort in cohorts)
        {
            var members = cohort.ToList();
            int size = members.Count;
            int cohortMonth = cohort.Key;

            var pcts = new int[6];
            for (var offset = 0; offset < 6; offset++)
            {
                int targetMonth = cohortMonth + offset;
                int retained = members.Count(m => m.Value.Contains(targetMonth));
                pcts[offset] = size > 0 ? (int)Math.Round(retained * 100.0 / size) : 0;
            }

            rows.Add(new CustomerCohortRowDto(MonthLabel(cohortMonth), size, pcts));
        }

        logger.LogInformation("Cohort retention hesaplandı. Kohort sayısı={Count}", rows.Count);
        return rows;
    }

    private static int MonthIndex(DateTimeOffset dto)
    {
        var d = dto.UtcDateTime;
        return d.Year * 12 + (d.Month - 1);
    }

    private static string MonthLabel(int monthIndex)
    {
        int year = monthIndex / 12;
        int month = (monthIndex % 12) + 1;
        return $"{year:0000}-{month:00}";
    }

    public async Task<IReadOnlyList<SendableCouponDto>> GetSendableCouponsAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var vouchers = await db.DiscountVouchers
            .Where(v => v.IsActive
                        && v.Code != null
                        && v.CustomerId == null          // şablon kupon (müşteriye henüz bağlanmamış)
                        && (v.ExpiringDate == null || v.ExpiringDate > now)
                        && (v.MaxUsageCount == null || v.CurrentUsageCount < v.MaxUsageCount))
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new
            {
                v.Id,
                v.Code,
                v.DiscountType,
                v.Percentage,
                v.Amount
            })
            .ToListAsync(ct);

        var tr = CultureInfo.GetCultureInfo("tr-TR");
        return vouchers.Select(v => new SendableCouponDto(
            v.Id,
            v.Code!,
            v.DiscountType == DiscountType.Percentage
                ? $"%{v.Percentage:0.##} indirim"
                : $"{v.Amount.ToString("N2", tr)} ₺ indirim")).ToList();
    }

    public async Task<IResult> SendRecoveryCouponAsync(
        SendRecoveryCouponDto dto, Guid? userId, CancellationToken ct = default)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // 2. Business rules — şablon kupon var/aktif mi, hedef müşteriler mevcut mu.
        var template = await db.DiscountVouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == dto.VoucherId, ct);

        var existingCustomerIds = await db.Customers
            .Where(c => dto.CustomerIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);

        var rule = LogicRunner.Run(
            template is null
                ? new ErrorResult("Seçilen indirim kuponu bulunamadı.")
                : new SuccessResult(),
            template is { IsActive: false }
                ? new ErrorResult("Seçilen indirim kuponu pasif durumda.")
                : new SuccessResult(),
            existingCustomerIds.Count == 0
                ? new ErrorResult("Seçilen müşterilerden hiçbiri bulunamadı.")
                : new SuccessResult());

        if (rule != null)
        {
            logger.LogWarning("Geri kazanım kuponu gönderimi iş kuralı ihlali: {Message}", rule.Message);
            return new ErrorResult(rule.Message!);
        }

        // 3. Execution — her müşteriye şablonun bir kopyası (CustomerId bağlı) oluşturulur.
        await applicationLogManager.AddLog(
            $"Geri kazanım indirim kodu gönderiliyor. Şablon: {template!.Code} · {existingCustomerIds.Count} müşteri · Segment: {dto.Segment ?? "-"}",
            LogType.DiscountVoucher, LogAction.Add, dto, ct);
        logger.LogInformation(
            "Sending recovery coupon. Template={TemplateId} Customers={Count} Segment={Segment}",
            dto.VoucherId, existingCustomerIds.Count, dto.Segment);

        foreach (var customerId in existingCustomerIds)
        {
            string code = await GenerateUniqueCodeAsync(db, ct);
            db.DiscountVouchers.Add(new DiscountVoucher
            {
                Code = code,
                DiscountType = template.DiscountType,
                Amount = template.Amount,
                Percentage = template.Percentage,
                ExpiringDate = template.ExpiringDate,
                MinimumCartAmount = template.MinimumCartAmount,
                MaxUsageCount = template.MaxUsageCount,
                CustomerId = customerId,
                IsActive = true
            });
        }

        // Yeni entity'ler Add ile track edildi → SaveChanges sessiz no-op olmaz.
        await db.SaveChangesAsync(ct);

        await applicationLogManager.AddLog(
            $"Geri kazanım indirim kodu {existingCustomerIds.Count} müşteriye gönderildi.",
            LogType.DiscountVoucher, LogAction.Add, token: ct);
        logger.LogInformation("Recovery coupon sent to {Count} customers.", existingCustomerIds.Count);

        return new SuccessDataResult<int>(
            existingCustomerIds.Count,
            $"{existingCustomerIds.Count} müşteriye indirim kodu gönderildi.");
    }

    private async Task<string> GenerateUniqueCodeAsync(IntegrationDbContext db, CancellationToken ct)
    {
        string code = randomGenerator.GetRandomCode(CodeLength, true, true, false);
        while (await db.DiscountVouchers.AnyAsync(x => x.Code == code, ct))
            code = randomGenerator.GetRandomCode(CodeLength, true, true, false);
        return code;
    }
}

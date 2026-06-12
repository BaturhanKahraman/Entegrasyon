using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Notifications.Emails;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Yüksek iadeli ürünün tedarikçisine bildirim — MUTASYON. 3-adım pipeline:
/// FluentValidation → iş kuralları (ürün/marka var mı) → execution (e-posta veya in-app) + çift log.
/// </summary>
public class SupplierReturnNotificationManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator fluentValidator,
    IStorefrontEmailService emailService,
    INotificationManager notificationManager,
    IApplicationLogManager applicationLogManager,
    ILogger<SupplierReturnNotificationManager> logger) : ISupplierReturnNotificationManager
{
    public async Task<IResult> NotifyHighReturnAsync(
        NotifySupplierHighReturnDto dto, Guid? userId, CancellationToken ct = default)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // 2. Business rules — ürün ve markası mevcut mu (tedarikçi = marka).
        var info = await db.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == dto.ProductVariantId)
            .Select(v => new
            {
                Title = v.Product.Title,
                BrandId = v.Product.BrandId,
                BrandName = v.Product.Brand != null ? v.Product.Brand.Name : null,
                BrandEmail = v.Product.Brand != null ? v.Product.Brand.SupplierEmail : null
            })
            .FirstOrDefaultAsync(ct);

        var rule = LogicRunner.Run(
            info is null
                ? new ErrorResult("Ürün bulunamadı.")
                : new SuccessResult(),
            info is { BrandId: null }
                ? new ErrorResult("Ürünün markası tanımlı değil; tedarikçi belirlenemedi.")
                : new SuccessResult());

        if (rule != null)
        {
            logger.LogWarning("Tedarikçi iade bildirimi iş kuralı ihlali: {Message}", rule.Message);
            return new ErrorResult(rule.Message!);
        }

        // 3. Execution
        var rate = dto.ReturnRatePercent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var subject = $"Yüksek İade Uyarısı — {info!.Title}";

        await applicationLogManager.AddLog(
            $"Yüksek iadeli ürün bildirimi hazırlanıyor. Ürün: {info.Title} · Marka: {info.BrandName} · İade oranı: %{rate}",
            LogType.Brand, LogAction.Add, dto, ct);

        if (!string.IsNullOrWhiteSpace(info.BrandEmail))
        {
            var body =
                $"<p>Sayın {info.BrandName} yetkilisi,</p>" +
                $"<p><strong>{info.Title}</strong> ürününde iade oranı <strong>%{rate}</strong> seviyesine ulaşmıştır. " +
                "Ürün kalitesi/beden uyumu açısından değerlendirmenizi rica ederiz.</p>" +
                "<p>İyi çalışmalar.</p>";

            var sent = await emailService.SendAsync(info.BrandEmail!, subject, body);
            if (!sent.Success)
            {
                logger.LogWarning("Tedarikçi e-postası gönderilemedi. Marka={Brand} Hata={Message}",
                    info.BrandName, sent.Message);
                return new ErrorResult($"E-posta gönderilemedi: {sent.Message}");
            }

            await applicationLogManager.AddLog(
                $"Yüksek iade bildirimi tedarikçiye e-posta ile gönderildi: {info.BrandEmail}", LogType.Brand, LogAction.Add, token: ct);
            logger.LogInformation("High-return supplier email sent. Brand={Brand} Variant={Variant}",
                info.BrandName, dto.ProductVariantId);

            return new SuccessResult($"Tedarikçiye e-posta gönderildi ({info.BrandEmail}).");
        }

        // E-posta yok → tetikleyen kullanıcıya in-app bildirim (panel) düşür.
        if (userId.HasValue)
        {
            await notificationManager.SendNotification(
                subject,
                $"{info.Title} ürününde iade oranı %{rate}. Markası ({info.BrandName}) için tedarikçi e-postası tanımlı değil — manuel iletişim gerekli.",
                NotificationSeverity.Warning,
                NotificationCategory.Urun,
                new[] { userId.Value },
                "/reports/returns");
        }

        await applicationLogManager.AddLog(
            $"Tedarikçi e-postası tanımlı değil ({info.BrandName}); bildirim panele düşürüldü.", LogType.Brand, LogAction.Add, token: ct);
        logger.LogInformation("High-return supplier email not configured; in-app fallback. Brand={Brand}", info.BrandName);

        return new SuccessResult(
            $"Tedarikçi e-postası tanımlı değil; bildirim panele düştü. {info.BrandName} markasına e-posta ekleyin.");
    }
}

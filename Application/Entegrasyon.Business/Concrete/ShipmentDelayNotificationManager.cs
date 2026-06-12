using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Gecikmiş gönderilerin müşterilerine toplu e-posta bilgilendirmesi — MUTASYON.
/// 3-adım pipeline: FluentValidation → iş kuralları (gönderi var mı) → execution (e-posta) + çift log.
/// E-postası olmayan gönderiler atlanır; tek bir e-posta hatası toplu işlemi durdurmaz.
/// </summary>
public class ShipmentDelayNotificationManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator fluentValidator,
    IStorefrontEmailService emailService,
    IApplicationLogManager applicationLogManager,
    ILogger<ShipmentDelayNotificationManager> logger) : IShipmentDelayNotificationManager
{
    public async Task<IResult> NotifyDelayedCustomersAsync(
        NotifyDelayedShipmentsDto dto, Guid? userId, CancellationToken ct = default)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // 2. Business rules — seçilen gönderiler mevcut mu.
        var shipments = await db.Set<ShipmentTracking>()
            .AsNoTracking()
            .Where(s => !s.IsDeleted && dto.ShipmentTrackingIds.Contains(s.Id))
            .Select(s => new
            {
                s.TrackingNumber,
                CargoName = s.CargoCompany.Name,
                s.EstimatedDeliveryDate,
                Email = s.Order != null ? s.Order.CustomerEmail : null,
                FirstName = s.Order != null ? s.Order.CustomerFirstName : null
            })
            .ToListAsync(ct);

        var rule = LogicRunner.Run(
            shipments.Count == 0
                ? new ErrorResult("Seçilen gönderiler bulunamadı.")
                : new SuccessResult());

        if (rule != null)
        {
            logger.LogWarning("Gecikme bilgilendirmesi iş kuralı ihlali: {Message}", rule.Message);
            return new ErrorResult(rule.Message!);
        }

        // 3. Execution
        await applicationLogManager.AddLog(
            $"Gecikmiş gönderi bilgilendirmesi başlatıldı. {shipments.Count} gönderi seçildi.",
            LogType.Order, LogAction.Add, dto, ct);

        int sent = 0, skipped = 0;
        foreach (var s in shipments)
        {
            if (string.IsNullOrWhiteSpace(s.Email))
            {
                skipped++;
                continue;
            }

            var eta = s.EstimatedDeliveryDate.HasValue
                ? s.EstimatedDeliveryDate.Value.ToString("dd.MM.yyyy")
                : "-";
            var subject = "Kargo Gecikme Bilgilendirmesi";
            var body =
                $"<p>Sayın {s.FirstName ?? "Müşterimiz"},</p>" +
                $"<p><strong>{s.TrackingNumber}</strong> takip numaralı {s.CargoName} kargonuzda gecikme yaşanmaktadır " +
                $"(tahmini teslim: {eta}). Anlayışınız için teşekkür eder, en kısa sürede teslimat için çalıştığımızı bildiririz.</p>";

            var res = await emailService.SendAsync(s.Email!, subject, body);
            if (res.Success) sent++;
            else skipped++;
        }

        await applicationLogManager.AddLog(
            $"Gecikme bilgilendirmesi tamamlandı: {sent} müşteriye e-posta gönderildi, {skipped} atlandı.",
            LogType.Order, LogAction.Add, token: ct);
        logger.LogInformation("Delay notification done. Sent={Sent} Skipped={Skipped}", sent, skipped);

        var msg = skipped > 0
            ? $"{sent} müşteri bilgilendirildi, {skipped} gönderi atlandı (e-posta yok/gönderilemedi)."
            : $"{sent} müşteri bilgilendirildi.";
        return new SuccessDataResult<int>(sent, msg);
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Shipping;

public class ShipmentTrackingManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IEnumerable<ICargoTrackingAdapter> adapters,
    IFluentValidator validator,
    IApplicationLogManager applicationLogManager,
    ILogger<ShipmentTrackingManager> logger) : IShipmentTrackingManager
{
    public async Task<IDataResult<ShipmentTrackingDto>> TrackShipmentAsync(TrackShipmentDto dto)
    {
        // 1. Validation
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorDataResult<ShipmentTrackingDto>(null!, errors);
        }

        // 2. Business Rules - find adapter
        var adapter = adapters.FirstOrDefault(a => a.CargoCompanyId == dto.CargoCompanyId);
        if (adapter == null)
        {
            logger.LogWarning("Kargo Şirketine uygun adapter bulunamadı: {CargoCompanyId}", dto.CargoCompanyId);
            return new ErrorDataResult<ShipmentTrackingDto>(null!,
                $"CargoCompanyId={dto.CargoCompanyId} icin uygun kargo adapter bulunamadı");
        }

        // 3. Execution
        try
        {
            var trackingResult = await adapter.GetTrackingInfoAsync(dto.TrackingNumber);
            if (!trackingResult.Success)
                return trackingResult;

            // Persist to database
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var existing = await dbContext.ShipmentTrackings
                .FirstOrDefaultAsync(x => x.TrackingNumber == dto.TrackingNumber && x.CargoCompanyId == dto.CargoCompanyId);

            if (existing == null)
            {
                var entity = new ShipmentTracking
                {
                    TrackingNumber = dto.TrackingNumber,
                    CargoCompanyId = dto.CargoCompanyId,
                    CurrentStatus = trackingResult.Data!.CurrentStatus,
                    LastStatusUpdate = DateTimeOffset.UtcNow,
                    EstimatedDeliveryDate = trackingResult.Data.EstimatedDeliveryDate,
                    ActualDeliveryDate = trackingResult.Data.ActualDeliveryDate,
                    RecipientName = trackingResult.Data.RecipientName,
                    RecipientAddress = trackingResult.Data.RecipientAddress
                };

                foreach (var history in trackingResult.Data.StatusHistories)
                {
                    entity.StatusHistory.Add(new ShipmentStatusHistory
                    {
                        Status = history.Status,
                        StatusDescription = history.Description,
                        Location = history.Location,
                        Timestamp = history.Timestamp
                    });
                }

                dbContext.ShipmentTrackings.Add(entity);
                await dbContext.SaveChangesAsync();

                var cargoCompany = await dbContext.CargoCompanies.FindAsync(dto.CargoCompanyId);

                var resultDto = trackingResult.Data with
                {
                    Id = entity.Id,
                    CargoCompanyName = cargoCompany?.Name ?? trackingResult.Data.CargoCompanyName
                };

                await applicationLogManager.AddLog(
                    $"Yeni kargo takibi eklendi: {dto.TrackingNumber}",
                    LogType.Order, LogAction.Add);

                return new SuccessDataResult<ShipmentTrackingDto>(resultDto);
            }

            // Update existing
            existing.CurrentStatus = trackingResult.Data!.CurrentStatus;
            existing.LastStatusUpdate = DateTimeOffset.UtcNow;
            existing.ActualDeliveryDate = trackingResult.Data.ActualDeliveryDate;
            await dbContext.SaveChangesAsync();

            var company = await dbContext.CargoCompanies.FindAsync(dto.CargoCompanyId);
            var updatedDto = trackingResult.Data with
            {
                Id = existing.Id,
                CargoCompanyName = company?.Name ?? trackingResult.Data.CargoCompanyName
            };

            return new SuccessDataResult<ShipmentTrackingDto>(updatedDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo takip hatasi: {TrackingNumber}", dto.TrackingNumber);
            return new ErrorDataResult<ShipmentTrackingDto>(null!, $"Kargo takip hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<Pageable<ShipmentTrackingDto>>> GetAllShipmentsAsync(ShipmentPaginatedRequest request)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var query = dbContext.ShipmentTrackings
                .Include(x => x.CargoCompany)
                .Include(x => x.StatusHistory)
                .AsQueryable();

            if (request.Status.HasValue)
                query = query.Where(x => x.CurrentStatus == request.Status.Value);

            if (request.CargoCompanyId.HasValue)
                query = query.Where(x => x.CargoCompanyId == request.CargoCompanyId.Value);

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            if (totalCount == 0)
                return new SuccessDataResult<Pageable<ShipmentTrackingDto>>(
                    new Pageable<ShipmentTrackingDto>([], request.PageIndex, request.PageSize, 0));

            var shipments = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = shipments.Select(s => new ShipmentTrackingDto(
                s.Id,
                s.OrderId,
                s.CargoCompanyId,
                s.CargoCompany?.Name ?? "",
                s.TrackingNumber,
                s.CurrentStatus,
                s.LastStatusUpdate,
                s.EstimatedDeliveryDate,
                s.ActualDeliveryDate,
                s.RecipientName,
                s.RecipientAddress,
                s.StatusHistory.Select(h => new ShipmentStatusHistoryDto(
                    h.Status, h.StatusDescription, h.Location, h.Timestamp
                )).OrderBy(h => h.Timestamp).ToList()
            )).ToList();

            return new SuccessDataResult<Pageable<ShipmentTrackingDto>>(
                new Pageable<ShipmentTrackingDto>(dtos, request.PageIndex, request.PageSize, totalCount));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo listesi getirme hatasi");
            return new ErrorDataResult<Pageable<ShipmentTrackingDto>>(null!, $"Kargo listesi getirme hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetShipmentHistoryAsync(long shipmentTrackingId)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var histories = await dbContext.ShipmentStatusHistories
                .Where(x => x.ShipmentTrackingId == shipmentTrackingId)
                .OrderBy(x => x.Timestamp)
                .Select(h => new ShipmentStatusHistoryDto(
                    h.Status, h.StatusDescription, h.Location, h.Timestamp))
                .ToListAsync();

            return new SuccessDataResult<List<ShipmentStatusHistoryDto>>(histories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo hareket gecmisi hatasi: {ShipmentTrackingId}", shipmentTrackingId);
            return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, $"Hareket gecmisi hatasi: {ex.Message}");
        }
    }

    public async Task<IResult> RefreshTrackingStatusAsync(long shipmentTrackingId)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var tracking = await dbContext.ShipmentTrackings
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == shipmentTrackingId);

            if (tracking == null)
                return new ErrorResult("Kargo takip kaydi bulunamadı");

            var adapter = adapters.FirstOrDefault(a => a.CargoCompanyId == tracking.CargoCompanyId);
            if (adapter == null)
                return new ErrorResult($"CargoCompanyId={tracking.CargoCompanyId} icin adapter bulunamadı");

            var historyResult = await adapter.GetStatusHistoryAsync(tracking.TrackingNumber);
            if (!historyResult.Success)
                return new ErrorResult(historyResult.Message ?? "Durum guncelleme başarısız");

            if (historyResult.Data != null && historyResult.Data.Count > 0)
            {
                var latestStatus = historyResult.Data.OrderByDescending(h => h.Timestamp).First();
                tracking.CurrentStatus = latestStatus.Status;
                tracking.LastStatusUpdate = DateTimeOffset.UtcNow;

                if (latestStatus.Status == ShipmentStatus.Delivered)
                    tracking.ActualDeliveryDate = latestStatus.Timestamp;

                // Add new history entries
                var existingTimestamps = tracking.StatusHistory.Select(h => h.Timestamp).ToHashSet();
                foreach (var history in historyResult.Data)
                {
                    if (!existingTimestamps.Contains(history.Timestamp))
                    {
                        tracking.StatusHistory.Add(new ShipmentStatusHistory
                        {
                            Status = history.Status,
                            StatusDescription = history.Description,
                            Location = history.Location,
                            Timestamp = history.Timestamp
                        });
                    }
                }

                await dbContext.SaveChangesAsync();

                await applicationLogManager.AddLog(
                    $"Kargo durumu guncellendi: {tracking.TrackingNumber} -> {tracking.CurrentStatus}",
                    LogType.Order, LogAction.Update);
            }

            return new SuccessResult("Kargo durumu guncellendi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo durum guncelleme hatasi: {ShipmentTrackingId}", shipmentTrackingId);
            return new ErrorResult($"Durum guncelleme hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<CargoSummaryDto>> GetCargoSummaryAsync()
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var shipments = await dbContext.ShipmentTrackings.ToListAsync();

            var summary = new CargoSummaryDto(
                TotalShipments: shipments.Count,
                InTransitCount: shipments.Count(s =>
                    s.CurrentStatus == ShipmentStatus.InTransit ||
                    s.CurrentStatus == ShipmentStatus.OutForDelivery ||
                    s.CurrentStatus == ShipmentStatus.PickedUp),
                DeliveredCount: shipments.Count(s => s.CurrentStatus == ShipmentStatus.Delivered),
                FailedCount: shipments.Count(s => s.CurrentStatus == ShipmentStatus.Failed),
                PendingCount: shipments.Count(s => s.CurrentStatus == ShipmentStatus.Created));

            return new SuccessDataResult<CargoSummaryDto>(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo ozet bilgisi hatasi");
            return new ErrorDataResult<CargoSummaryDto>(null!, $"Kargo ozet bilgisi hatasi: {ex.Message}");
        }
    }

    public async Task<ShipmentKpiDto> GetShipmentKpisAsync(CancellationToken ct = default)
    {
        // Kargo liste sayfası üst KPI snapshot'ı — tablo filtresinden bağımsız, tüm
        // (silinmemiş, query filter) sevkiyatlar. Tek server-side GroupBy(CurrentStatus) →
        // sunucuda count; kovalara bellekte pivot (grup sayısı enum kadar ≤8).
        // NOT: GetCargoSummaryAsync'in aksine tüm satırları BELLEĞE ÇEKMEZ (full-load yok).
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var byStatus = await dbContext.ShipmentTrackings
            .GroupBy(s => s.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountFor(params ShipmentStatus[] statuses)
            => byStatus.Where(x => statuses.Contains(x.Status)).Sum(x => x.Count);

        return new ShipmentKpiDto(
            InTransitCount: CountFor(ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery),
            DeliveredCount: CountFor(ShipmentStatus.Delivered),
            ProblemCount: CountFor(ShipmentStatus.Failed, ShipmentStatus.ReturnedToSender));
    }
}

using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Trendyol;

public sealed record TrendyolBatchResponse(string BatchRequestId);

public sealed record TrendyolBatchStatusResponse(
    string BatchRequestId,
    TrendyolBatchStatus Status,
    List<TrendyolBatchItem>? Items,
    int ItemCount,
    int FailedItemCount,
    string? BatchRequestType,
    long? CreationDate,
    long? LastModification);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrendyolBatchStatus
{
    IN_PROGRESS,
    COMPLETED
}

public sealed record TrendyolBatchItem(
    object? RequestItem,
    string Status,
    List<string>? FailureReasons);

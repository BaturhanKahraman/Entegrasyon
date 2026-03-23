using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

// ── Product requests ─────────────────────────────────────────────────────────

public sealed record CiceksepetiCreateProductsRequest(
    [property: JsonPropertyName("Products")] List<CiceksepetiProductRequest> Products);

public sealed record CiceksepetiProductRequest(
    [property: JsonPropertyName("ProductName")] string ProductName,
    [property: JsonPropertyName("ProductCode")] string? ProductCode,
    [property: JsonPropertyName("CategoryId")] int CategoryId,
    [property: JsonPropertyName("StockCode")] string StockCode,
    [property: JsonPropertyName("MainProductCode")] string MainProductCode,
    [property: JsonPropertyName("Description")] string? Description,
    [property: JsonPropertyName("SalesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("ListPrice")] decimal ListPrice,
    [property: JsonPropertyName("StockQuantity")] int StockQuantity,
    [property: JsonPropertyName("Barcode")] string? Barcode,
    [property: JsonPropertyName("Images")] List<string>? Images,
    [property: JsonPropertyName("Attributes")] List<CiceksepetiAttributeRequest>? Attributes);

public sealed record CiceksepetiAttributeRequest(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("ValueId")] int ValueId,
    [property: JsonPropertyName("TextLength")] int TextLength);

// ── Stock / Price requests ────────────────────────────────────────────────────

public sealed record CiceksepetiStockPriceUpdateRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiStockPriceItem> Items);

public sealed record CiceksepetiStockPriceItem(
    [property: JsonPropertyName("StockCode")] string StockCode,
    [property: JsonPropertyName("StockQuantity")] int? StockQuantity,
    [property: JsonPropertyName("SalesPrice")] decimal? SalesPrice,
    [property: JsonPropertyName("ListPrice")] decimal? ListPrice);

// ── Order requests ────────────────────────────────────────────────────────────

public sealed record CiceksepetiGetOrdersRequest(
    [property: JsonPropertyName("StartDate")] string? StartDate,
    [property: JsonPropertyName("EndDate")] string? EndDate,
    [property: JsonPropertyName("PageSize")] int PageSize,
    [property: JsonPropertyName("Page")] int Page,
    [property: JsonPropertyName("StatusId")] int? StatusId,
    [property: JsonPropertyName("OrderNo")] long? OrderNo,
    [property: JsonPropertyName("OrderItemNo")] long? OrderItemNo);

// ── Cargo requests ────────────────────────────────────────────────────────────

public sealed record CiceksepetiCsCargoRequest(
    [property: JsonPropertyName("CargoGroups")] List<CiceksepetiCargoGroup> CargoGroups);

public sealed record CiceksepetiCargoGroup(
    [property: JsonPropertyName("OrderItemIds")] List<long> OrderItemIds);

public sealed record CiceksepetiOwnCargoRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiOwnCargoItem> Items);

public sealed record CiceksepetiOwnCargoItem(
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("CargoCompany")] string CargoCompany,
    [property: JsonPropertyName("TrackingNumber")] string TrackingNumber);

public sealed record CiceksepetiChangeCargoRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiChangeCargoItem> Items);

public sealed record CiceksepetiChangeCargoItem(
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("CargoCompany")] string CargoCompany,
    [property: JsonPropertyName("TrackingNumber")] string TrackingNumber);

public sealed record CiceksepetiCargoMeasurementRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiCargoMeasurementItem> Items);

public sealed record CiceksepetiCargoMeasurementItem(
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("Desi")] decimal Desi,
    [property: JsonPropertyName("Weight")] decimal Weight);

public sealed record CiceksepetiDigitalCodeRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiDigitalCodeItem> Items);

public sealed record CiceksepetiDigitalCodeItem(
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("DigitalCode")] string DigitalCode);

public sealed record CiceksepetiLaborCostRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiLaborCostItem> Items);

public sealed record CiceksepetiLaborCostItem(
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("LaborCost")] decimal LaborCost);

// ── Invoice requests ──────────────────────────────────────────────────────────

public sealed record CiceksepetiInvoiceRequest(
    [property: JsonPropertyName("Items")] List<CiceksepetiInvoiceItem> Items);

public sealed record CiceksepetiInvoiceItem(
    [property: JsonPropertyName("OrderItemId")] int OrderItemId,
    [property: JsonPropertyName("Document")] string? Document,
    [property: JsonPropertyName("DocumentUrl")] string? DocumentUrl);

// ── Return requests ───────────────────────────────────────────────────────────

public sealed record CiceksepetiGetReturnsRequest(
    [property: JsonPropertyName("StartDate")] string? StartDate,
    [property: JsonPropertyName("EndDate")] string? EndDate,
    [property: JsonPropertyName("PageSize")] int PageSize,
    [property: JsonPropertyName("Page")] int Page,
    [property: JsonPropertyName("StatusId")] int? StatusId);

public sealed record CiceksepetiReturnReceivedRequest(
    [property: JsonPropertyName("OrderItemIds")] List<int> OrderItemIds);

public sealed record CiceksepetiReturnEvaluationRequest(
    [property: JsonPropertyName("OrderItemId")] int OrderItemId,
    [property: JsonPropertyName("Process")] int Process);

// ── Q&A requests ──────────────────────────────────────────────────────────────

public sealed record CiceksepetiAnswerQuestionRequest(
    [property: JsonPropertyName("Answer")] string? Answer,
    [property: JsonPropertyName("BranchActionId")] int BranchActionId,
    [property: JsonPropertyName("BranchActionDetailId")] int? BranchActionDetailId,
    [property: JsonPropertyName("BranchDescription")] string? BranchDescription);

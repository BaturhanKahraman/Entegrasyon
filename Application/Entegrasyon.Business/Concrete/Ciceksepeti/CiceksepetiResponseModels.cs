using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

// ── Category models ──────────────────────────────────────────────────────────

public sealed record CiceksepetiCategoryResponse(
    [property: JsonPropertyName("Categories")] List<CiceksepetiCategoryDto> Categories);

public sealed record CiceksepetiCategoryDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("ParentCategoryId")] int? ParentCategoryId,
    [property: JsonPropertyName("SubCategories")] List<CiceksepetiCategoryDto> SubCategories);

public sealed record CiceksepetiCategoryAttributeResponse(
    [property: JsonPropertyName("CategoryId")] int CategoryId,
    [property: JsonPropertyName("CategoryName")] string CategoryName,
    [property: JsonPropertyName("CategoryAttributes")] List<CiceksepetiAttributeDto> CategoryAttributes);

public sealed record CiceksepetiAttributeDto(
    [property: JsonPropertyName("AttributeId")] int AttributeId,
    [property: JsonPropertyName("AttributeName")] string AttributeName,
    [property: JsonPropertyName("Required")] bool Required,
    [property: JsonPropertyName("Varianter")] bool Varianter,
    [property: JsonPropertyName("Type")] string Type,
    [property: JsonPropertyName("AttributeValues")] List<CiceksepetiAttributeValueDto> AttributeValues);

public sealed record CiceksepetiAttributeValueDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("Name")] string Name);

// ── Product / Batch models ───────────────────────────────────────────────────

public sealed record CiceksepetiBatchResponse(
    [property: JsonPropertyName("BatchId")] string BatchId);

public sealed record CiceksepetiBatchStatusResponse(
    [property: JsonPropertyName("BatchId")] string BatchId,
    [property: JsonPropertyName("ItemCount")] int ItemCount,
    [property: JsonPropertyName("Items")] List<CiceksepetiBatchItemDto> Items);

public sealed record CiceksepetiBatchItemDto(
    [property: JsonPropertyName("Data")] CiceksepetiBatchItemData? Data,
    [property: JsonPropertyName("ItemId")] string ItemId,
    [property: JsonPropertyName("Status")] string Status,
    [property: JsonPropertyName("FailureReasons")] List<CiceksepetiFailureReason>? FailureReasons,
    [property: JsonPropertyName("LastModificationDate")] string? LastModificationDate);

public sealed record CiceksepetiBatchItemData(
    [property: JsonPropertyName("SiteCode")] string? SiteCode,
    [property: JsonPropertyName("StockCode")] string? StockCode,
    [property: JsonPropertyName("StockQuantity")] int? StockQuantity,
    [property: JsonPropertyName("ListPrice")] decimal? ListPrice,
    [property: JsonPropertyName("SalesPrice")] decimal? SalesPrice);

public sealed record CiceksepetiFailureReason(
    [property: JsonPropertyName("Message")] string Message,
    [property: JsonPropertyName("Code")] string? Code);

public sealed record CiceksepetiProductListResponse(
    [property: JsonPropertyName("TotalCount")] int TotalCount,
    [property: JsonPropertyName("Products")] List<CiceksepetiProductDto> Products);

public sealed record CiceksepetiProductDto(
    [property: JsonPropertyName("ProductName")] string ProductName,
    [property: JsonPropertyName("ProductCode")] string? ProductCode,
    [property: JsonPropertyName("CategoryId")] int CategoryId,
    [property: JsonPropertyName("CategoryName")] string? CategoryName,
    [property: JsonPropertyName("StockCode")] string StockCode,
    [property: JsonPropertyName("MainProductCode")] string MainProductCode,
    [property: JsonPropertyName("ProductStatusType")] int ProductStatusType,
    [property: JsonPropertyName("Description")] string? Description,
    [property: JsonPropertyName("Link")] string? Link,
    [property: JsonPropertyName("SalesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("StockQuantity")] int StockQuantity,
    [property: JsonPropertyName("Barcode")] string? Barcode,
    [property: JsonPropertyName("IsActive")] bool IsActive,
    [property: JsonPropertyName("Images")] List<string>? Images,
    [property: JsonPropertyName("Attributes")] List<CiceksepetiProductAttributeDto>? Attributes);

public sealed record CiceksepetiProductAttributeDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("ValueId")] int ValueId,
    [property: JsonPropertyName("TextLength")] int TextLength);

// ── Order models ─────────────────────────────────────────────────────────────

public sealed record CiceksepetiOrderListResponse(
    [property: JsonPropertyName("OrderListCount")] int OrderListCount,
    [property: JsonPropertyName("SupplierOrderListWithBranch")] List<CiceksepetiOrderItemDto> SupplierOrderListWithBranch);

public sealed record CiceksepetiOrderItemDto(
    [property: JsonPropertyName("BranchId")] int BranchId,
    [property: JsonPropertyName("OrderId")] long OrderId,
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("OrderItemStatusId")] int OrderItemStatusId,
    [property: JsonPropertyName("OrderDate")] string? OrderDate,
    [property: JsonPropertyName("ProductName")] string? ProductName,
    [property: JsonPropertyName("ProductCode")] string? ProductCode,
    [property: JsonPropertyName("StockCode")] string? StockCode,
    [property: JsonPropertyName("Quantity")] int Quantity,
    [property: JsonPropertyName("SalesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("ListPrice")] decimal ListPrice,
    [property: JsonPropertyName("InvoicePrice")] decimal InvoicePrice,
    [property: JsonPropertyName("AllowanceRate")] decimal AllowanceRate,
    [property: JsonPropertyName("ReceiverName")] string? ReceiverName,
    [property: JsonPropertyName("ReceiverAddress")] string? ReceiverAddress,
    [property: JsonPropertyName("ReceiverCity")] string? ReceiverCity,
    [property: JsonPropertyName("ReceiverDistrict")] string? ReceiverDistrict,
    [property: JsonPropertyName("ReceiverPhone")] string? ReceiverPhone,
    [property: JsonPropertyName("SenderName")] string? SenderName,
    [property: JsonPropertyName("CargoCompany")] string? CargoCompany,
    [property: JsonPropertyName("CargoTrackingNumber")] string? CargoTrackingNumber,
    [property: JsonPropertyName("CargoTrackingUrl")] string? CargoTrackingUrl,
    [property: JsonPropertyName("DeliveryType")] int DeliveryType,
    [property: JsonPropertyName("DeliveryMessageType")] int DeliveryMessageType,
    [property: JsonPropertyName("Barcode")] string? Barcode,
    [property: JsonPropertyName("CancellationResult")] int? CancellationResult,
    [property: JsonPropertyName("Note")] string? Note);

// ── Return models ────────────────────────────────────────────────────────────

public sealed record CiceksepetiReturnListResponse(
    [property: JsonPropertyName("OrderItemList")] List<CiceksepetiReturnItemDto> OrderItemList);

public sealed record CiceksepetiReturnItemDto(
    [property: JsonPropertyName("OrderId")] long OrderId,
    [property: JsonPropertyName("OrderItemId")] long OrderItemId,
    [property: JsonPropertyName("OrderItemStatusId")] int OrderItemStatusId,
    [property: JsonPropertyName("CustomerName")] string? CustomerName,
    [property: JsonPropertyName("SalesPrice")] decimal SalesPrice,
    [property: JsonPropertyName("CancelReason")] string? CancelReason,
    [property: JsonPropertyName("CancelStatusId")] int? CancelStatusId,
    [property: JsonPropertyName("CargoCompany")] string? CargoCompany,
    [property: JsonPropertyName("CargoTrackingNumber")] string? CargoTrackingNumber,
    [property: JsonPropertyName("ProductName")] string? ProductName,
    [property: JsonPropertyName("StockCode")] string? StockCode);

// ── Q&A models ───────────────────────────────────────────────────────────────

public sealed record CiceksepetiQuestionListResponse(
    [property: JsonPropertyName("Items")] List<CiceksepetiQuestionDto> Items,
    [property: JsonPropertyName("HasNextPage")] bool HasNextPage);

public sealed record CiceksepetiQuestionDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("Question")] string Question,
    [property: JsonPropertyName("Answer")] string? Answer,
    [property: JsonPropertyName("Answered")] bool Answered,
    [property: JsonPropertyName("CreatedDate")] string? CreatedDate,
    [property: JsonPropertyName("Product")] CiceksepetiQuestionProductDto? Product,
    [property: JsonPropertyName("BranchActionId")] int? BranchActionId,
    [property: JsonPropertyName("Approve")] bool? Approve);

public sealed record CiceksepetiQuestionProductDto(
    [property: JsonPropertyName("Code")] string Code,
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Url")] string? Url,
    [property: JsonPropertyName("ImageUrl")] string? ImageUrl);

public sealed record CiceksepetiActionListResponse(
    [property: JsonPropertyName("Actions")] List<CiceksepetiActionDto> Actions);

public sealed record CiceksepetiActionDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Details")] List<CiceksepetiActionDetailDto> Details);

public sealed record CiceksepetiActionDetailDto(
    [property: JsonPropertyName("Id")] int Id,
    [property: JsonPropertyName("Name")] string Name);

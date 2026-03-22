using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed record PazaramaResponse<T>(
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("messageCode")] string? MessageCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("userMessage")] string? UserMessage,
    [property: JsonPropertyName("fromCache")] bool FromCache);

public sealed record PazaramaCategoryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("parentId")] Guid? ParentId,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parentCategories")] List<string>? ParentCategories,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("displayOrder")] int DisplayOrder,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("leaf")] bool Leaf);

public sealed record PazaramaCategoryWithAttributesDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributes")] List<PazaramaCategoryAttributeDto> Attributes);

public sealed record PazaramaCategoryAttributeDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("isVariantable")] bool IsVariantable,
    [property: JsonPropertyName("isRequired")] bool IsRequired,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributeValues")] List<PazaramaCategoryAttributeValueDto> AttributeValues);

public sealed record PazaramaCategoryAttributeValueDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("value")] string Value);

public sealed record PazaramaBrandDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("logoUrl")] string? LogoUrl,
    [property: JsonPropertyName("website")] string? Website,
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("seoName")] string? SeoName);

// Batch status for product create
public sealed record PazaramaBatchStatusResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("batchRequestId")] string BatchRequestId,
    [property: JsonPropertyName("batchResult")] List<object>? BatchResult,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("successfulCount")] int SuccessfulCount,
    [property: JsonPropertyName("isExcel")] bool IsExcel,
    [property: JsonPropertyName("failedCount")] int FailedCount,
    [property: JsonPropertyName("failedProducts")] List<PazaramaFailedProduct>? FailedProducts,
    [property: JsonPropertyName("creationDate")] string? CreationDate);

public sealed record PazaramaFailedProduct(
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("productCode")] string ProductCode,
    [property: JsonPropertyName("errorReason")] string ErrorReason);

public sealed record PazaramaStockPriceBatchResponse(
    [property: JsonPropertyName("pageIndex")] int PageIndex,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("data")] List<PazaramaStockPriceBatchItem>? Data,
    [property: JsonPropertyName("successCount")] int SuccessCount,
    [property: JsonPropertyName("notCompletedCount")] int NotCompletedCount,
    [property: JsonPropertyName("failedCount")] int FailedCount,
    [property: JsonPropertyName("processingCount")] int ProcessingCount);

public sealed record PazaramaStockPriceBatchItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("price")] PazaramaPriceBatchDetail? Price,
    [property: JsonPropertyName("stock")] PazaramaStockBatchDetail? Stock,
    [property: JsonPropertyName("operationStatusText")] string OperationStatusText);

public sealed record PazaramaPriceBatchDetail(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("operationDetail")] string? OperationDetail,
    [property: JsonPropertyName("salePrice")] decimal SalePrice,
    [property: JsonPropertyName("listPrice")] decimal ListPrice);

public sealed record PazaramaStockBatchDetail(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("operationDetail")] string? OperationDetail,
    [property: JsonPropertyName("count")] int Count);

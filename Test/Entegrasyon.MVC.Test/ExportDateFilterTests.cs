using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.BulkOperations;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Test;

public class ExportDateFilterTests
{
    [Fact]
    public async Task ExportWithColumns_PassesFormDates_ToExportFilter()
    {
        // Arrange
        var fake = new CapturingBulkOperationManager();
        var controller = new BulkOperationController(fake);
        var dateFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await controller.ExportWithColumns(
            BulkOperationType.ProductExport,
            selectedColumns: ["Sku", "Name"],
            dateFrom: dateFrom,
            dateTo: dateTo);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        fake.CapturedFilter.Should().NotBeNull();
        fake.CapturedFilter!.DateFrom.Should().Be(dateFrom);
        fake.CapturedFilter!.DateTo.Should().Be(dateTo);
        fake.CapturedFilter!.SelectedColumns.Should().BeEquivalentTo("Sku", "Name");
    }

    [Fact]
    public async Task ExportWithColumns_WithoutDates_LeavesFilterDatesNull()
    {
        // Arrange — geriye dönük uyumluluk: tarih girilmezse tüm veri export edilmeli
        var fake = new CapturingBulkOperationManager();
        var controller = new BulkOperationController(fake);

        // Act
        await controller.ExportWithColumns(
            BulkOperationType.ProductExport,
            selectedColumns: null,
            dateFrom: null,
            dateTo: null);

        // Assert
        fake.CapturedFilter.Should().NotBeNull();
        fake.CapturedFilter!.DateFrom.Should().BeNull();
        fake.CapturedFilter!.DateTo.Should().BeNull();
    }

    private sealed class CapturingBulkOperationManager : IBulkOperationManager
    {
        public ExportFilterDto? CapturedFilter { get; private set; }

        public Task<IDataResult<byte[]>> ExportProductsAsync(ExportFilterDto filter, CancellationToken cancellationToken = default)
        {
            CapturedFilter = filter;
            return Task.FromResult<IDataResult<byte[]>>(new SuccessDataResult<byte[]>([1, 2, 3]));
        }

        public Task<IDataResult<byte[]>> ExportPricesAsync(ExportFilterDto filter, CancellationToken cancellationToken = default)
        {
            CapturedFilter = filter;
            return Task.FromResult<IDataResult<byte[]>>(new SuccessDataResult<byte[]>([1, 2, 3]));
        }

        public Task<IDataResult<byte[]>> ExportStockAsync(ExportFilterDto filter, CancellationToken cancellationToken = default)
        {
            CapturedFilter = filter;
            return Task.FromResult<IDataResult<byte[]>>(new SuccessDataResult<byte[]>([1, 2, 3]));
        }

        public Task<IDataResult<BulkImportResultDto>> ImportProductsAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null)
            => throw new NotImplementedException();
        public Task<IDataResult<BulkImportResultDto>> ImportPricesAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null)
            => throw new NotImplementedException();
        public Task<IDataResult<BulkImportResultDto>> ImportStockAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null)
            => throw new NotImplementedException();
        public Task<IDataResult<byte[]>> GetImportTemplateAsync(BulkOperationType type)
            => throw new NotImplementedException();
        public Task<IDataResult<ImportValidationPreviewDto>> ValidateImportAsync(Stream excelStream, BulkOperationType operationType)
            => throw new NotImplementedException();
        public Task<IDataResult<BulkOperationLog>> GetOperationLogAsync(long id)
            => throw new NotImplementedException();
        public Task<IDataResult<List<BulkOperationLog>>> GetRecentOperationsAsync(int count = 20)
            => throw new NotImplementedException();
        public Task<IDataResult<List<ExportColumnDto>>> GetAvailableColumnsAsync(BulkOperationType type)
            => throw new NotImplementedException();
    }
}

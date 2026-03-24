using Entegrasyon.Entity.Dtos.BulkOperations;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public class ProductImportValidator
{
    public List<BulkImportRowErrorDto> ValidateProductRows(List<ProductImportRow> rows)
    {
        var errors = new List<BulkImportRowErrorDto>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Barcode))
            {
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Barkod boş olamaz."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Title))
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Ürün adı boş olamaz."));

            if (row.ListPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Liste fiyatı negatif olamaz."));

            if (row.SalePrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı negatif olamaz."));

            if (row.CostPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Maliyet fiyatı negatif olamaz."));

            if (row.VatRate < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "KDV oranı negatif olamaz."));
        }

        return errors;
    }

    public List<BulkImportRowErrorDto> ValidatePriceRows(List<PriceImportRow> rows)
    {
        var errors = new List<BulkImportRowErrorDto>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Barcode))
            {
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Barkod boş olamaz."));
                continue;
            }

            if (row.ListPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Liste fiyatı negatif olamaz."));

            if (row.SalePrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı negatif olamaz."));

            if (row.CostPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Maliyet fiyatı negatif olamaz."));
        }

        return errors;
    }

    public List<BulkImportRowErrorDto> ValidateStockRows(List<StockImportRow> rows)
    {
        var errors = new List<BulkImportRowErrorDto>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Barcode))
            {
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Barkod boş olamaz."));
                continue;
            }

            if (row.BranchOfficeId <= 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Şube ID geçersiz."));

            if (row.Quantity < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Stok miktarı negatif olamaz."));
        }

        return errors;
    }
}

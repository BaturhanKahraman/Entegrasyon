using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.BulkOperations;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public class ProductImportValidator : IProductImportValidator
{
    private static readonly HashSet<decimal> ValidVatRates = [0, 1, 2, 4, 8, 10, 18, 20];

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

            if (row.Barcode.Length > 50)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Barkod 50 karakterden uzun olamaz."));

            if (string.IsNullOrWhiteSpace(row.Title))
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Ürün adı boş olamaz."));
            else if (row.Title.Length > 500)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Ürün adı 500 karakterden uzun olamaz."));

            if (row.ListPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Liste fiyatı negatif olamaz."));
            else if (row.ListPrice == 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Liste fiyatı sıfırdan büyük olmalı."));

            if (row.SalePrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı negatif olamaz."));

            if (row.CostPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Maliyet fiyatı negatif olamaz."));

            if (row.VatRate < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "KDV oranı negatif olamaz."));
            else if (!ValidVatRates.Contains(row.VatRate))
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, $"KDV oranı geçersiz. Geçerli değerler: {string.Join(", ", ValidVatRates.OrderBy(x => x).Select(x => $"%{x:0}"))}"));

            if (row.ListPrice > 0 && row.SalePrice > row.ListPrice)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı liste fiyatından büyük olamaz."));
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
            else if (row.ListPrice == 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Liste fiyatı sıfırdan büyük olmalı."));

            if (row.SalePrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı negatif olamaz."));

            if (row.CostPrice < 0)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Maliyet fiyatı negatif olamaz."));

            if (row.ListPrice > 0 && row.SalePrice > row.ListPrice)
                errors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Satış fiyatı liste fiyatından büyük olamaz."));
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

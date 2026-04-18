using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class CreateUnifiedSalesView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW vw_unified_sales AS
SELECT
    s.""Id""                                  AS ""Id"",
    0                                         AS ""EntityType"",
    CASE s.""SaleSource""
        WHEN 1 THEN 1
        WHEN 2 THEN 2
        ELSE 2
    END                                       AS ""Source"",
    s.""SaleNumber""                          AS ""Number"",
    s.""SaleDate""                            AS ""SaleDate"",
    s.""CustomerId""                          AS ""CustomerId"",
    CASE
        WHEN s.""CustomerId"" IS NULL THEN NULL
        ELSE COALESCE(c.""FullName"", TRIM(CONCAT(c.""Name"", ' ', c.""Surname"")), 'Müşteri')
    END                                       AS ""CustomerDisplayName"",
    COALESCE((SELECT SUM(si.""UnitPrice"" * si.""Quantity"")
              FROM ""SaleItems"" si
              WHERE si.""SaleId"" = s.""Id"" AND si.""IsDeleted"" = false), 0)
                                              AS ""TotalPrice"",
    COALESCE((SELECT SUM(si.""Quantity"")
              FROM ""SaleItems"" si
              WHERE si.""SaleId"" = s.""Id"" AND si.""IsDeleted"" = false), 0)
                                              AS ""ItemCount"",
    CAST(s.""SaleStatus"" AS int)             AS ""RawStatusCode"",
    NULL                                      AS ""CargoTrackingNumber"",
    NULL::int                                 AS ""MarketPlaceId""
FROM ""Sales"" s
LEFT JOIN ""Customers"" c ON c.""Id"" = s.""CustomerId"" AND c.""IsDeleted"" = false
WHERE s.""IsDeleted"" = false

UNION ALL

SELECT
    o.""Id""                                  AS ""Id"",
    1                                         AS ""EntityType"",
    CASE
        WHEN o.""MarketPlaceId"" IS NULL THEN 3
        ELSE 10 + o.""MarketPlaceId""
    END                                       AS ""Source"",
    o.""OrderNumber""                         AS ""Number"",
    COALESCE(o.""OrderDate"", o.""CreatedAt"")AS ""SaleDate"",
    o.""CustomerId""                          AS ""CustomerId"",
    COALESCE(
        NULLIF(TRIM(CONCAT(o.""CustomerFirstName"", ' ', o.""CustomerLastName"")), ''),
        c.""FullName"",
        NULLIF(TRIM(CONCAT(c.""Name"", ' ', c.""Surname"")), '')
    )                                         AS ""CustomerDisplayName"",
    COALESCE(o.""TotalPrice"", 0)             AS ""TotalPrice"",
    COALESCE(o.""TotalQuantity"", 0)          AS ""ItemCount"",
    COALESCE(CAST(o.""StorefrontOrderStatus"" AS int), 0)
                                              AS ""RawStatusCode"",
    o.""CargoTrackingNumber""                 AS ""CargoTrackingNumber"",
    o.""MarketPlaceId""                       AS ""MarketPlaceId""
FROM ""Orders"" o
LEFT JOIN ""Customers"" c ON c.""Id"" = o.""CustomerId"" AND c.""IsDeleted"" = false
WHERE o.""IsDeleted"" = false;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_unified_sales;");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class FixLeafNodeViolations : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Leaf node ihlallerini duzeltir:
        /// - Attribute'lari parent'tan leaf cocuklara kopyalar, parent'tan siler
        /// - Sync kayitlarini parent'tan deaktive eder
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Attribute ihlalleri: "Ilk Kategori" (id=1) ve "Gomlek" (id=10)
            // Attribute'lari leaf cocuklara kopyala (cocuklarin cocugu olmayanlar)
            migrationBuilder.Sql(@"
                INSERT INTO ""CategoryAttributeCategories"" (""CategoryId"", ""CategoryAttributeId"", ""IsRequired"", ""IsVarianter"", ""IsSlicer"", ""IsDeleted"", ""DeletedAt"", ""CreatedAt"", ""UpdatedAt"")
                SELECT child.""Id"", cac.""CategoryAttributeId"", cac.""IsRequired"", false, false, false, '0001-01-01T00:00:00Z', NOW(), NOW()
                FROM ""Categories"" child
                INNER JOIN ""CategoryAttributeCategories"" cac ON cac.""CategoryId"" = child.""SuperCategoryId""
                WHERE child.""SuperCategoryId"" IN (1, 10)
                  AND child.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM ""Categories"" grandchild
                      WHERE grandchild.""SuperCategoryId"" = child.""Id"" AND grandchild.""IsDeleted"" = false
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM ""CategoryAttributeCategories"" existing
                      WHERE existing.""CategoryId"" = child.""Id""
                        AND existing.""CategoryAttributeId"" = cac.""CategoryAttributeId""
                  );
            ");

            // 2. Parent'lardan attribute'lari sil
            migrationBuilder.Sql(@"
                DELETE FROM ""CategoryAttributeCategories""
                WHERE ""CategoryId"" IN (1, 10);
            ");

            // 3. Sync ihlalleri: "Akilli Telefon" (id=35) ve "Akilli Saat" (id=37)
            // Sync kayitlarini deaktive et
            migrationBuilder.Sql(@"
                UPDATE ""CategoryMarketplaces""
                SET ""IsActive"" = false
                WHERE ""CategoryId"" IN (35, 37) AND ""IsActive"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migration — geri alinmasi gerekirse manuel mudahale gerekir
        }
    }
}

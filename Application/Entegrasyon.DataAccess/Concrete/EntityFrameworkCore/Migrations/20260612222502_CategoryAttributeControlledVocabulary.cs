using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class CategoryAttributeControlledVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeKeyValues_CategoryAttributeValues_AttributeValueId",
                table: "AttributeKeyValues");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributeValues_CategoryAttributeId",
                table: "CategoryAttributeValues");

            migrationBuilder.DropColumn(
                name: "AllowCustom",
                table: "TemplateCategoryAttributeData");

            migrationBuilder.DropColumn(
                name: "CustomValue",
                table: "ProductVariantAttributes");

            migrationBuilder.DropColumn(
                name: "AllowCustom",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "CustomValue",
                table: "AttributeKeyValues");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CategoryAttributeValues",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // --- Apply-güvenli veri hazırlığı (mevcut dev satırları yeni kısıtları ihlal etmesin) ---
            // Korektlik re-seed ile gelir (NormalizedName = AttributeValueNormalizer.Normalize); buradaki
            // upper() yalnızca migration'ın mevcut satırlarda çökmeden uygulanması içindir.

            // 1) NormalizedName backfill: trim + iç boşluk teke + upper.
            migrationBuilder.Sql(@"
                UPDATE ""CategoryAttributeValues""
                SET ""NormalizedName"" = upper(regexp_replace(btrim(""Name""), '\s+', ' ', 'g'))
                WHERE ""Name"" IS NOT NULL;");

            // 2) Dedup: aktif (silinmemiş) satırlarda (CategoryAttributeId, NormalizedName) çakışmasında
            //    en küçük Id'yi koru, diğerlerini soft-delete et (fiziksel silme yok → FK referansları korunur).
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT ""Id"",
                           row_number() OVER (PARTITION BY ""CategoryAttributeId"", ""NormalizedName""
                                              ORDER BY ""Id"") AS rn
                    FROM ""CategoryAttributeValues""
                    WHERE ""IsDeleted"" = false
                )
                UPDATE ""CategoryAttributeValues"" v
                SET ""IsDeleted"" = true
                FROM ranked r
                WHERE v.""Id"" = r.""Id"" AND r.rn > 1;");

            // 3) AttributeValueId artık zorunlu (non-null FK). Eski serbest-metin (CustomValue) kayıtları
            //    AttributeValueId IS NULL idi; karşılığı bir değer olmadığından bu link'leri sil
            //    (AlterColumn NOT NULL + FK ihlalini önler; re-seed/yeniden giriş doğru değerle kurar).
            migrationBuilder.Sql(@"
                DELETE FROM ""AttributeKeyValues"" WHERE ""AttributeValueId"" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "AttributeValueId",
                table: "AttributeKeyValues",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributeValues_CategoryAttributeId_NormalizedName",
                table: "CategoryAttributeValues",
                columns: new[] { "CategoryAttributeId", "NormalizedName" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeKeyValues_CategoryAttributeValues_AttributeValueId",
                table: "AttributeKeyValues",
                column: "AttributeValueId",
                principalTable: "CategoryAttributeValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeKeyValues_CategoryAttributeValues_AttributeValueId",
                table: "AttributeKeyValues");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributeValues_CategoryAttributeId_NormalizedName",
                table: "CategoryAttributeValues");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CategoryAttributeValues");

            migrationBuilder.AddColumn<bool>(
                name: "AllowCustom",
                table: "TemplateCategoryAttributeData",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomValue",
                table: "ProductVariantAttributes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCustom",
                table: "CategoryAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "AttributeValueId",
                table: "AttributeKeyValues",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CustomValue",
                table: "AttributeKeyValues",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributeValues_CategoryAttributeId",
                table: "CategoryAttributeValues",
                column: "CategoryAttributeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeKeyValues_CategoryAttributeValues_AttributeValueId",
                table: "AttributeKeyValues",
                column: "AttributeValueId",
                principalTable: "CategoryAttributeValues",
                principalColumn: "Id");
        }
    }
}

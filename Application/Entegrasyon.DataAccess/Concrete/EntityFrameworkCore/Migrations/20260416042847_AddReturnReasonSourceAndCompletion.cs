using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnReasonSourceAndCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_NormalizedName",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "SaleReturns",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReturnedByUserId",
                table: "SaleReturns",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "SaleReturns",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "SaleReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "SaleReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "SaleReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByUserId",
                table: "SaleReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomReason",
                table: "SaleReturns",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Preserve existing ReturnReason string data into new CustomReason column
            migrationBuilder.Sql(
                "UPDATE \"SaleReturns\" SET \"CustomReason\" = NULLIF(\"ReturnReason\", '') WHERE \"ReturnReason\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "SaleReturns");

            migrationBuilder.AddColumn<int>(
                name: "MarketplaceId",
                table: "SaleReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceReturnId",
                table: "SaleReturns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "SaleReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestoreBranchOfficeId",
                table: "SaleReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnReasonId",
                table: "SaleReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "SaleReturns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleItemId",
                table: "SaleReturnItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "OrderItemId",
                table: "SaleReturnItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RestoredAt",
                table: "SaleReturnItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestoredByUserId",
                table: "SaleReturnItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestoredToStock",
                table: "SaleReturnItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: previously Approved returns had their stock auto-restored by the old ApproveReturnAsync
            // Mark those items as RestoredToStock=true so the new Complete flow doesn't double-add stock
            migrationBuilder.Sql(
                @"UPDATE ""SaleReturnItems"" sri
                  SET ""RestoredToStock"" = true, ""RestoredAt"" = sr.""UpdatedAt""
                  FROM ""SaleReturns"" sr
                  WHERE sri.""SaleReturnId"" = sr.""Id"" AND sr.""ReturnStatus"" = 2;");

            migrationBuilder.AddColumn<int>(
                name: "ReturnedQuantity",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReturnReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReasons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "IsActive", "IsDeleted", "IsSystem", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "SIZE_MISMATCH", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Beden uymuyor", 10, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "DEFECTIVE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Hatalı ürün", 20, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "DAMAGED_IN_SHIPPING", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Kargoda hasar gördü", 30, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "CUSTOMER_CHANGED_MIND", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Müşteri vazgeçti", 40, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, "WRONG_ITEM_SENT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Yanlış ürün gönderildi", 50, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, "PRICE_DIFFERENCE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Fiyat farkı", 60, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7, "OTHER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, true, "Diğer", 99, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CancelledByUserId",
                table: "SaleReturns",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CompletedAt",
                table: "SaleReturns",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CompletedByUserId",
                table: "SaleReturns",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_OrderId",
                table: "SaleReturns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_RestoreBranchOfficeId",
                table: "SaleReturns",
                column: "RestoreBranchOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_ReturnReasonId",
                table: "SaleReturns",
                column: "ReturnReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_ReturnStatus",
                table: "SaleReturns",
                column: "ReturnStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_Source",
                table: "SaleReturns",
                column: "Source");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleReturn_SaleOrOrder",
                table: "SaleReturns",
                sql: "(\"SaleId\" IS NOT NULL AND \"OrderId\" IS NULL) OR (\"SaleId\" IS NULL AND \"OrderId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_OrderItemId",
                table: "SaleReturnItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_RestoredByUserId",
                table: "SaleReturnItems",
                column: "RestoredByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleReturnItem_SaleOrOrder",
                table: "SaleReturnItems",
                sql: "(\"SaleItemId\" IS NOT NULL AND \"OrderItemId\" IS NULL) OR (\"SaleItemId\" IS NULL AND \"OrderItemId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands",
                column: "SeoSlug",
                unique: true,
                filter: "\"SeoSlug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReasons_Code",
                table: "ReturnReasons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReasons_IsActive",
                table: "ReturnReasons",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturnItems_OrderItems_OrderItemId",
                table: "SaleReturnItems",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturnItems_Users_RestoredByUserId",
                table: "SaleReturnItems",
                column: "RestoredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_BranchOffices_RestoreBranchOfficeId",
                table: "SaleReturns",
                column: "RestoreBranchOfficeId",
                principalTable: "BranchOffices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_Orders_OrderId",
                table: "SaleReturns",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_ReturnReasons_ReturnReasonId",
                table: "SaleReturns",
                column: "ReturnReasonId",
                principalTable: "ReturnReasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_Users_CancelledByUserId",
                table: "SaleReturns",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_Users_CompletedByUserId",
                table: "SaleReturns",
                column: "CompletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturnItems_OrderItems_OrderItemId",
                table: "SaleReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturnItems_Users_RestoredByUserId",
                table: "SaleReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_BranchOffices_RestoreBranchOfficeId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_Orders_OrderId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_ReturnReasons_ReturnReasonId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_Users_CancelledByUserId",
                table: "SaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_Users_CompletedByUserId",
                table: "SaleReturns");

            migrationBuilder.DropTable(
                name: "ReturnReasons");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_CancelledByUserId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_CompletedAt",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_CompletedByUserId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_OrderId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_RestoreBranchOfficeId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_ReturnReasonId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_ReturnStatus",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_Source",
                table: "SaleReturns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleReturn_SaleOrOrder",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturnItems_OrderItemId",
                table: "SaleReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturnItems_RestoredByUserId",
                table: "SaleReturnItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleReturnItem_SaleOrOrder",
                table: "SaleReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "CustomReason",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "MarketplaceId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "MarketplaceReturnId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "RestoreBranchOfficeId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "ReturnReasonId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "RestoredAt",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "RestoredByUserId",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "RestoredToStock",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "OrderItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "SaleReturns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReturnedByUserId",
                table: "SaleReturns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "SaleReturns",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleItemId",
                table: "SaleReturnItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_NormalizedName",
                table: "Brands",
                column: "NormalizedName",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands",
                column: "SeoSlug",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}

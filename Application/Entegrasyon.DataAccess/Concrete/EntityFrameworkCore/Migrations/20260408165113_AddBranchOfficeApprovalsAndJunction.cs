using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchOfficeApprovalsAndJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastSelectedBranchOfficeId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RememberLastBranchOffice",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BranchOffices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "BranchOffices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletionRequestId",
                table: "BranchOffices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHeadquarters",
                table: "BranchOffices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "BranchOffices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchOfficeDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchOfficeId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TransferTargetBranchOfficeId = table.Column<int>(type: "integer", nullable: true),
                    HasStockTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchOfficeDeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchOfficeDeletionRequests_BranchOffices_BranchOfficeId",
                        column: x => x.BranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchOfficeDeletionRequests_BranchOffices_TransferTargetBr~",
                        column: x => x.TransferTargetBranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchOfficeDeletionRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchOfficeDeletionRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceBranchOfficeId = table.Column<int>(type: "integer", nullable: false),
                    TargetBranchOfficeId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferRequests_BranchOffices_SourceBranchOfficeId",
                        column: x => x.SourceBranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferRequests_BranchOffices_TargetBranchOfficeId",
                        column: x => x.TargetBranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBranchOffices",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchOfficeId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranchOffices", x => new { x.UserId, x.BranchOfficeId });
                    table.ForeignKey(
                        name: "FK_UserBranchOffices_BranchOffices_BranchOfficeId",
                        column: x => x.BranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBranchOffices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchOfficeDeletionRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeletionRequestId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchOfficeDeletionRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchOfficeDeletionRequestItems_BranchOfficeDeletionReques~",
                        column: x => x.DeletionRequestId,
                        principalTable: "BranchOfficeDeletionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferRequestId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferRequestItems_StockTransferRequests_TransferReq~",
                        column: x => x.TransferRequestId,
                        principalTable: "StockTransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BranchOffices",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "DeletionRequestId", "IsHeadquarters", "NormalizedName" },
                values: new object[] { null, null, true, "MERKEZ OFIS" });

            // Backfill: user-created ofislerin NormalizedName'i — PostgreSQL TRANSLATE ile Türkçe karakterleri
            // ASCII eşdeğerlerine çevir + TRIM + UPPER. BranchNameNormalizer'ın yaklaşık SQL karşılığı.
            // (combining marks için yetersiz ama Türkçe karakterler için tam kapsar)
            migrationBuilder.Sql(@"
                UPDATE ""BranchOffices""
                SET ""NormalizedName"" = UPPER(TRIM(TRANSLATE(
                    ""Name"",
                    'ıİşŞğĞüÜöÖçÇ',
                    'iIsSgGuUoOcC'
                )))
                WHERE ""NormalizedName"" IS NULL AND ""IsDeleted"" = false;
            ");

            // Backfill: mevcut Users.DefaultBranchOfficeId → UserBranchOffices junction
            // Her kullanıcının mevcut birincil ofisi junction'a da eklenir (backward-compatible atama).
            migrationBuilder.Sql(@"
                INSERT INTO ""UserBranchOffices"" (""UserId"", ""BranchOfficeId"", ""AssignedAt"")
                SELECT ""Id"", ""DefaultBranchOfficeId"", NOW()
                FROM ""Users""
                WHERE ""DefaultBranchOfficeId"" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM ""UserBranchOffices"" ubo
                    WHERE ubo.""UserId"" = ""Users"".""Id""
                      AND ubo.""BranchOfficeId"" = ""Users"".""DefaultBranchOfficeId""
                  );
            ");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"),
                columns: new[] { "LastSelectedBranchOfficeId", "RememberLastBranchOffice" },
                values: new object[] { null, false });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastSelectedBranchOfficeId",
                table: "Users",
                column: "LastSelectedBranchOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOffices_DeletionRequestId",
                table: "BranchOffices",
                column: "DeletionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOffices_NormalizedName",
                table: "BranchOffices",
                column: "NormalizedName",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequestItems_DeletionRequestId",
                table: "BranchOfficeDeletionRequestItems",
                column: "DeletionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequests_ApprovedByUserId",
                table: "BranchOfficeDeletionRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequests_BranchOfficeId_Status",
                table: "BranchOfficeDeletionRequests",
                columns: new[] { "BranchOfficeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequests_RequestedByUserId",
                table: "BranchOfficeDeletionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequests_Status",
                table: "BranchOfficeDeletionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeDeletionRequests_TransferTargetBranchOfficeId",
                table: "BranchOfficeDeletionRequests",
                column: "TransferTargetBranchOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequestItems_TransferRequestId",
                table: "StockTransferRequestItems",
                column: "TransferRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_ApprovedByUserId",
                table: "StockTransferRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_RequestedByUserId",
                table: "StockTransferRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_SourceBranchOfficeId_Status",
                table: "StockTransferRequests",
                columns: new[] { "SourceBranchOfficeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_Status",
                table: "StockTransferRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_TargetBranchOfficeId_Status",
                table: "StockTransferRequests",
                columns: new[] { "TargetBranchOfficeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchOffices_BranchOfficeId",
                table: "UserBranchOffices",
                column: "BranchOfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchOffices_BranchOfficeDeletionRequests_DeletionRequestId",
                table: "BranchOffices",
                column: "DeletionRequestId",
                principalTable: "BranchOfficeDeletionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_BranchOffices_LastSelectedBranchOfficeId",
                table: "Users",
                column: "LastSelectedBranchOfficeId",
                principalTable: "BranchOffices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchOffices_BranchOfficeDeletionRequests_DeletionRequestId",
                table: "BranchOffices");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_BranchOffices_LastSelectedBranchOfficeId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "BranchOfficeDeletionRequestItems");

            migrationBuilder.DropTable(
                name: "StockTransferRequestItems");

            migrationBuilder.DropTable(
                name: "UserBranchOffices");

            migrationBuilder.DropTable(
                name: "BranchOfficeDeletionRequests");

            migrationBuilder.DropTable(
                name: "StockTransferRequests");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastSelectedBranchOfficeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_BranchOffices_DeletionRequestId",
                table: "BranchOffices");

            migrationBuilder.DropIndex(
                name: "IX_BranchOffices_NormalizedName",
                table: "BranchOffices");

            migrationBuilder.DropColumn(
                name: "LastSelectedBranchOfficeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RememberLastBranchOffice",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "BranchOffices");

            migrationBuilder.DropColumn(
                name: "DeletionRequestId",
                table: "BranchOffices");

            migrationBuilder.DropColumn(
                name: "IsHeadquarters",
                table: "BranchOffices");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "BranchOffices");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BranchOffices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}

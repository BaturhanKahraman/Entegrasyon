using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEFaturaRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EFaturaRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EnvelopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApiStatusCode = table.Column<int>(type: "integer", nullable: true),
                    PdfDownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LocalReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayableAmountKurus = table.Column<long>(type: "bigint", nullable: false),
                    TaxAmountKurus = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SentToGibAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvoiceLinkSentToMarketplace = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EFaturaRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EFaturaRecords_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MarketPlaces",
                columns: new[] { "Id", "ApiKey", "ApiSecret", "BaseUrl", "BasicAuthPassword", "BasicAuthUserName", "CreatedAt", "DeletedAt", "IsBasicAuth", "IsDeleted", "Name", "RefreshToken", "SellerId", "TokenUrl", "UpdatedAt", "UserAgentPrefix" },
                values: new object[] { 8, null, null, "https://apis.ciceksepeti.com", null, null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, false, "Çiçeksepeti", null, null, null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.CreateIndex(
                name: "IX_EFaturaRecords_InvoiceUuid",
                table: "EFaturaRecords",
                column: "InvoiceUuid");

            migrationBuilder.CreateIndex(
                name: "IX_EFaturaRecords_OrderId",
                table: "EFaturaRecords",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EFaturaRecords_Status",
                table: "EFaturaRecords",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EFaturaRecords");

            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}

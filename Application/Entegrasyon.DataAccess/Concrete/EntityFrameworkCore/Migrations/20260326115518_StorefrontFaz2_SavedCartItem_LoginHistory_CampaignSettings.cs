using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class StorefrontFaz2_SavedCartItem_LoginHistory_CampaignSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoReviewRewardEnabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoReviewRewardPercent",
                table: "StorefrontSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoWelcomeCouponEnabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoWelcomeCouponPercent",
                table: "StorefrontSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorefrontLoginHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthId = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "text", nullable: true),
                    LoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontLoginHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontSavedCartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontSavedCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorefrontSavedCartItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontLoginHistories_AuthId",
                table: "StorefrontLoginHistories",
                column: "AuthId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontLoginHistories_LoginAt",
                table: "StorefrontLoginHistories",
                column: "LoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontSavedCartItems_ProductVariantId",
                table: "StorefrontSavedCartItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontSavedCartItems_TenantId_CustomerId_ProductVariant~",
                table: "StorefrontSavedCartItems",
                columns: new[] { "TenantId", "CustomerId", "ProductVariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontLoginHistories");

            migrationBuilder.DropTable(
                name: "StorefrontSavedCartItems");

            migrationBuilder.DropColumn(
                name: "AutoReviewRewardEnabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AutoReviewRewardPercent",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AutoWelcomeCouponEnabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AutoWelcomeCouponPercent",
                table: "StorefrontSettings");
        }
    }
}

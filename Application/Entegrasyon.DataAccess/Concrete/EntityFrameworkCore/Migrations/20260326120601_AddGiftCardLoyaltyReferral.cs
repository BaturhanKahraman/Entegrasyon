using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftCardLoyaltyReferral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoyaltyMinRedemption",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsPerLira",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsRedemptionRate",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LoyaltyProgramEnabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyReferralBonus",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyReviewBonus",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyWelcomeBonus",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StorefrontGiftCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InitialAmount = table.Column<decimal>(type: "money", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "money", nullable: false),
                    PurchasedByCustomerId = table.Column<int>(type: "integer", nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SenderMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontGiftCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontLoyaltyPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    CurrentBalance = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontLoyaltyPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontLoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontLoyaltyTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontReferrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ReferrerCustomerId = table.Column<int>(type: "integer", nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ReferredCustomerId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontReferrals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontGiftCardTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "money", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "money", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "money", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontGiftCardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorefrontGiftCardTransactions_StorefrontGiftCards_GiftCard~",
                        column: x => x.GiftCardId,
                        principalTable: "StorefrontGiftCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontGiftCards_TenantId_Code",
                table: "StorefrontGiftCards",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontGiftCards_TenantId_Status",
                table: "StorefrontGiftCards",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontGiftCardTransactions_GiftCardId",
                table: "StorefrontGiftCardTransactions",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontGiftCardTransactions_OrderId",
                table: "StorefrontGiftCardTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontLoyaltyPoints_TenantId_CustomerId",
                table: "StorefrontLoyaltyPoints",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontLoyaltyTransactions_TenantId_CustomerId",
                table: "StorefrontLoyaltyTransactions",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontReferrals_TenantId_ReferralCode",
                table: "StorefrontReferrals",
                columns: new[] { "TenantId", "ReferralCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontReferrals_TenantId_ReferrerCustomerId",
                table: "StorefrontReferrals",
                columns: new[] { "TenantId", "ReferrerCustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontGiftCardTransactions");

            migrationBuilder.DropTable(
                name: "StorefrontLoyaltyPoints");

            migrationBuilder.DropTable(
                name: "StorefrontLoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "StorefrontReferrals");

            migrationBuilder.DropTable(
                name: "StorefrontGiftCards");

            migrationBuilder.DropColumn(
                name: "LoyaltyMinRedemption",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsPerLira",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsRedemptionRate",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyProgramEnabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyReferralBonus",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyReviewBonus",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyWelcomeBonus",
                table: "StorefrontSettings");
        }
    }
}

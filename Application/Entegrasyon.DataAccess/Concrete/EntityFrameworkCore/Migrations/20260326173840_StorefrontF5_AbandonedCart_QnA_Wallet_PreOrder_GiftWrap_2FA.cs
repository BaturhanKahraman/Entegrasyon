using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class StorefrontF5_AbandonedCart_QnA_Wallet_PreOrder_GiftWrap_2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbandonedCartEmail1Hours",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AbandonedCartEmail2Hours",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AbandonedCartEmail3DiscountPercent",
                table: "StorefrontSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AbandonedCartEmail3Enabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AbandonedCartRecoveryEnabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GiftWrappingEnabled",
                table: "StorefrontSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GiftWrappingFee",
                table: "StorefrontSettings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "StorefrontCustomerAuths",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "StorefrontCustomerAuths",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GiftMessage",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiftWrappingFee",
                table: "Orders",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideInvoice",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGiftWrapped",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreOrder",
                table: "MainProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreOrderEstimatedDate",
                table: "MainProducts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreOrderLimit",
                table: "MainProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorefrontAbandonedCartEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    EmailStep = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Converted = table.Column<bool>(type: "boolean", nullable: false),
                    CouponCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontAbandonedCartEmails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontProductQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AnswerText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontProductQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontTwoFactorRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontTwoFactorRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorefrontTwoFactorRecoveryCodes_StorefrontCustomerAuths_Au~",
                        column: x => x.AuthId,
                        principalTable: "StorefrontCustomerAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "money", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalletId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "money", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BalanceBefore = table.Column<decimal>(type: "money", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "money", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorefrontWalletTransactions_StorefrontWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "StorefrontWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontAbandonedCartEmails_Status",
                table: "StorefrontAbandonedCartEmails",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontAbandonedCartEmails_TenantId_CartId_EmailStep",
                table: "StorefrontAbandonedCartEmails",
                columns: new[] { "TenantId", "CartId", "EmailStep" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontAbandonedCartEmails_TenantId_CustomerId",
                table: "StorefrontAbandonedCartEmails",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductQuestions_TenantId_IsPublished",
                table: "StorefrontProductQuestions",
                columns: new[] { "TenantId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontProductQuestions_TenantId_ProductId",
                table: "StorefrontProductQuestions",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontTwoFactorRecoveryCodes_AuthId",
                table: "StorefrontTwoFactorRecoveryCodes",
                column: "AuthId");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontWallets_TenantId_CustomerId",
                table: "StorefrontWallets",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontWalletTransactions_WalletId",
                table: "StorefrontWalletTransactions",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontAbandonedCartEmails");

            migrationBuilder.DropTable(
                name: "StorefrontProductQuestions");

            migrationBuilder.DropTable(
                name: "StorefrontTwoFactorRecoveryCodes");

            migrationBuilder.DropTable(
                name: "StorefrontWalletTransactions");

            migrationBuilder.DropTable(
                name: "StorefrontWallets");

            migrationBuilder.DropColumn(
                name: "AbandonedCartEmail1Hours",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AbandonedCartEmail2Hours",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AbandonedCartEmail3DiscountPercent",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AbandonedCartEmail3Enabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "AbandonedCartRecoveryEnabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "GiftWrappingEnabled",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "GiftWrappingFee",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "StorefrontCustomerAuths");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "StorefrontCustomerAuths");

            migrationBuilder.DropColumn(
                name: "GiftMessage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GiftWrappingFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HideInvoice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsGiftWrapped",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsPreOrder",
                table: "MainProducts");

            migrationBuilder.DropColumn(
                name: "PreOrderEstimatedDate",
                table: "MainProducts");

            migrationBuilder.DropColumn(
                name: "PreOrderLimit",
                table: "MainProducts");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultReceiptTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var thermalJson = """
            [
              { "id": "b-logo",   "type": "logo",        "showInNormal": true,  "showInGift": true,  "settings": {} },
              { "id": "b-store",  "type": "store_info",  "showInNormal": true,  "showInGift": true,  "settings": { "align": "center", "size": "m" } },
              { "id": "b-div1",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
              { "id": "b-meta",   "type": "meta",        "showInNormal": true,  "showInGift": true,  "settings": { "showSaleNumber": true, "showDate": true, "showCashier": true, "showCustomer": true } },
              { "id": "b-div2",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
              { "id": "b-items",  "type": "items",       "showInNormal": true,  "showInGift": true,  "settings": { "showBarcode": false, "showVatColumn": false } },
              { "id": "b-div3",   "type": "divider",     "showInNormal": true,  "showInGift": false, "settings": { "style": "dashed", "color": "black" } },
              { "id": "b-totals", "type": "totals",      "showInNormal": true,  "showInGift": false, "settings": {} },
              { "id": "b-vat",    "type": "vat_summary", "showInNormal": true,  "showInGift": false, "settings": {} },
              { "id": "b-pay",    "type": "payments",    "showInNormal": true,  "showInGift": false, "settings": {} },
              { "id": "b-div4",   "type": "divider",     "showInNormal": true,  "showInGift": true,  "settings": { "style": "dashed", "color": "black" } },
              { "id": "b-return", "type": "return_code", "showInNormal": true,  "showInGift": true,  "settings": { "showLabel": true } },
              { "id": "b-thanks", "type": "text",        "showInNormal": true,  "showInGift": true,  "settings": { "content": "Teşekkür ederiz!", "align": "center", "bold": false, "size": "m" } }
            ]
            """;

            var a4Json = """
            {
              "hl": [ { "id": "a-logo", "type": "logo", "showInNormal": true, "showInGift": true, "settings": {} } ],
              "hr": [ { "id": "a-store", "type": "store_info", "showInNormal": true, "showInGift": true, "settings": { "align": "left", "size": "m" } } ],
              "body": [
                { "id": "a-divb",   "type": "divider",     "showInNormal": true, "showInGift": true, "settings": { "style": "solid", "color": "black" } },
                { "id": "a-meta",   "type": "meta",        "showInNormal": true, "showInGift": true, "settings": { "showSaleNumber": true, "showDate": true, "showCashier": true, "showCustomer": true } },
                { "id": "a-items",  "type": "items",       "showInNormal": true, "showInGift": true, "settings": { "showBarcode": true, "showVatColumn": true } },
                { "id": "a-totals", "type": "totals",      "showInNormal": true, "showInGift": false, "settings": {} },
                { "id": "a-vat",    "type": "vat_summary", "showInNormal": true, "showInGift": false, "settings": {} }
              ],
              "fl": [
                { "id": "a-pay",    "type": "payments",    "showInNormal": true, "showInGift": false, "settings": {} },
                { "id": "a-divf",   "type": "divider",     "showInNormal": true, "showInGift": true,  "settings": { "style": "solid", "color": "black" } },
                { "id": "a-return", "type": "return_code", "showInNormal": true, "showInGift": true,  "settings": { "showLabel": true } }
              ],
              "fr": [
                { "id": "a-thanks", "type": "text", "showInNormal": true, "showInGift": true, "settings": { "content": "Teşekkür ederiz!", "align": "right", "bold": false, "size": "m" } },
                { "id": "a-sign",   "type": "text", "showInNormal": true, "showInGift": false, "settings": { "content": "Müşteri imzası: ______________", "align": "right", "bold": false, "size": "s" } }
              ]
            }
            """;

            migrationBuilder.InsertData(
                table: "ReceiptTemplates",
                columns: ["Id", "ThermalJson", "A4Json", "LogoUrl", "LogoWidthPx", "StoreName", "StoreAddress", "StorePhone", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt"],
                values: [1, thermalJson, a4Json, null, 120, "", "", "", false, DateTimeOffset.MinValue, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ReceiptTemplates\" WHERE \"Id\" = 1;");
        }
    }
}

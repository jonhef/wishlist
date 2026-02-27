using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFxRatesAndMinorUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "base_currency",
                table: "wishlists",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<int>(
                name: "price_amount_minor",
                table: "wish_items",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE wish_items
                SET price_amount_minor = CASE
                  WHEN price_amount IS NULL THEN NULL
                  WHEN price_currency = 'JPY' THEN ROUND(price_amount)::integer
                  ELSE ROUND(price_amount * 100)::integer
                END;

                UPDATE wish_items
                SET price_currency = NULL, price_amount_minor = NULL
                WHERE price_currency IS NOT NULL AND price_currency NOT IN ('EUR','USD','RUB','JPY');
                """);

            migrationBuilder.DropColumn(
                name: "price_amount",
                table: "wish_items");

            migrationBuilder.RenameColumn(
                name: "price_amount_minor",
                table: "wish_items",
                newName: "price_amount");

            migrationBuilder.CreateTable(
                name: "fx_rates",
                columns: table => new
                {
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    rate_to_base = table.Column<decimal>(type: "numeric(20,10)", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_rates", x => new { x.base_currency, x.quote_currency, x.as_of });
                    table.CheckConstraint("CK_fx_rates_base_currency_supported", "base_currency IN ('EUR','USD','RUB','JPY')");
                    table.CheckConstraint("CK_fx_rates_quote_currency_supported", "quote_currency IN ('EUR','USD','RUB','JPY')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_wishlists_base_currency_supported",
                table: "wishlists",
                sql: "base_currency IN ('EUR','USD','RUB','JPY')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wish_items_price_currency_supported",
                table: "wish_items",
                sql: "price_currency IS NULL OR price_currency IN ('EUR','USD','RUB','JPY')");

            migrationBuilder.CreateIndex(
                name: "IX_fx_rates_base_currency_quote_currency_as_of",
                table: "fx_rates",
                columns: new[] { "base_currency", "quote_currency", "as_of" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fx_rates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wishlists_base_currency_supported",
                table: "wishlists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wish_items_price_currency_supported",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "base_currency",
                table: "wishlists");

            migrationBuilder.AddColumn<decimal>(
                name: "price_amount_major",
                table: "wish_items",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE wish_items
                SET price_amount_major = CASE
                  WHEN price_amount IS NULL THEN NULL
                  WHEN price_currency = 'JPY' THEN price_amount::numeric(18,2)
                  ELSE ROUND((price_amount::numeric(18,2) / 100), 2)
                END;
                """);

            migrationBuilder.DropColumn(
                name: "price_amount",
                table: "wish_items");

            migrationBuilder.RenameColumn(
                name: "price_amount_major",
                table: "wish_items",
                newName: "price_amount");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistItemsCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wish_items_WishlistId_Id",
                table: "wish_items");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "wish_items",
                newName: "Name");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "wish_items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "wish_items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "wish_items",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAmount",
                table: "wish_items",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                table: "wish_items",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "wish_items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "wish_items",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "wish_items",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE wish_items
                SET UpdatedAtUtc = CreatedAtUtc
                WHERE UpdatedAtUtc = '0001-01-01 00:00:00';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_wish_items_WishlistId_UpdatedAtUtc_Id",
                table: "wish_items",
                columns: new[] { "WishlistId", "UpdatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wish_items_WishlistId_UpdatedAtUtc_Id",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "wish_items");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "wish_items",
                newName: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_wish_items_WishlistId_Id",
                table: "wish_items",
                columns: new[] { "WishlistId", "Id" });
        }
    }
}

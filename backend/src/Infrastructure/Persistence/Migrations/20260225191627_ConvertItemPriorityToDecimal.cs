using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertItemPriorityToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wish_items_WishlistId_UpdatedAtUtc_Id",
                table: "wish_items");

            migrationBuilder.AlterColumn<decimal>(
                name: "Priority",
                table: "wish_items",
                type: "numeric(38,18)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.Sql("""
                UPDATE wish_items
                SET "Priority" = "Priority" * 1024;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_wish_items_WishlistId_Priority_CreatedAtUtc_Id",
                table: "wish_items",
                columns: new[] { "WishlistId", "Priority", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wish_items_WishlistId_Priority_CreatedAtUtc_Id",
                table: "wish_items");

            migrationBuilder.Sql("""
                UPDATE wish_items
                SET "Priority" = ROUND("Priority" / 1024);
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "wish_items",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(38,18)");

            migrationBuilder.CreateIndex(
                name: "IX_wish_items_WishlistId_UpdatedAtUtc_Id",
                table: "wish_items",
                columns: new[] { "WishlistId", "UpdatedAtUtc", "Id" });
        }
    }
}

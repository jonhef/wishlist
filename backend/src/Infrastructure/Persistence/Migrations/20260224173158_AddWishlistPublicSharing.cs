using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistPublicSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareTokenHash",
                table: "wishlists",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_ShareTokenHash",
                table: "wishlists",
                column: "ShareTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wishlists_ShareTokenHash",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "ShareTokenHash",
                table: "wishlists");
        }
    }
}

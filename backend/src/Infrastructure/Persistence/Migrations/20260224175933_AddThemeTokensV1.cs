using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeTokensV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tokens",
                table: "themes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_themes_OwnerUserId_CreatedAtUtc_Id",
                table: "themes",
                columns: new[] { "OwnerUserId", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_themes_OwnerUserId_CreatedAtUtc_Id",
                table: "themes");

            migrationBuilder.DropColumn(
                name: "Tokens",
                table: "themes");
        }
    }
}

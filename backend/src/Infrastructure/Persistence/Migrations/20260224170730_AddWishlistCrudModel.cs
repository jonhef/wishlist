using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistCrudModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wishlists_OwnerUserId_Name",
                table: "wishlists");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "wishlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "wishlists",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "wishlists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ThemeId",
                table: "wishlists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "wishlists",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE wishlists
                SET UpdatedAtUtc = CreatedAtUtc
                WHERE UpdatedAtUtc = '0001-01-01 00:00:00';
                """);

            migrationBuilder.CreateTable(
                name: "themes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_themes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_themes_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_OwnerUserId_UpdatedAtUtc_Id",
                table: "wishlists",
                columns: new[] { "OwnerUserId", "UpdatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_ThemeId",
                table: "wishlists",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_themes_OwnerUserId_Name",
                table: "themes",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_wishlists_themes_ThemeId",
                table: "wishlists",
                column: "ThemeId",
                principalTable: "themes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wishlists_themes_ThemeId",
                table: "wishlists");

            migrationBuilder.DropTable(
                name: "themes");

            migrationBuilder.DropIndex(
                name: "IX_wishlists_OwnerUserId_UpdatedAtUtc_Id",
                table: "wishlists");

            migrationBuilder.DropIndex(
                name: "IX_wishlists_ThemeId",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "ThemeId",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "wishlists");

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_OwnerUserId_Name",
                table: "wishlists",
                columns: new[] { "OwnerUserId", "Name" });
        }
    }
}

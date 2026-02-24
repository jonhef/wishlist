using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistOwnershipAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WishlistId",
                table: "wish_items",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wishlists_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wish_items_WishlistId_Id",
                table: "wish_items",
                columns: new[] { "WishlistId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_wishlists_OwnerUserId_Name",
                table: "wishlists",
                columns: new[] { "OwnerUserId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_wish_items_wishlists_WishlistId",
                table: "wish_items",
                column: "WishlistId",
                principalTable: "wishlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_wish_items_wishlists_WishlistId",
                table: "wish_items");

            migrationBuilder.DropTable(
                name: "wishlists");

            migrationBuilder.DropIndex(
                name: "IX_wish_items_WishlistId_Id",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "WishlistId",
                table: "wish_items");
        }
    }
}

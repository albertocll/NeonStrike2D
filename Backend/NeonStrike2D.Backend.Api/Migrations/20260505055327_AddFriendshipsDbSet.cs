using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeonStrike2D.Backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendshipsDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_Users_AddresseeId",
                table: "Friendship");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_Users_RequesterId",
                table: "Friendship");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Friendship",
                table: "Friendship");

            migrationBuilder.RenameTable(
                name: "Friendship",
                newName: "Friendships");

            migrationBuilder.RenameIndex(
                name: "IX_Friendship_RequesterId",
                table: "Friendships",
                newName: "IX_Friendships_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_Friendship_AddresseeId",
                table: "Friendships",
                newName: "IX_Friendships_AddresseeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Friendships",
                table: "Friendships",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_AddresseeId",
                table: "Friendships",
                column: "AddresseeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_RequesterId",
                table: "Friendships",
                column: "RequesterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_AddresseeId",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_RequesterId",
                table: "Friendships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Friendships",
                table: "Friendships");

            migrationBuilder.RenameTable(
                name: "Friendships",
                newName: "Friendship");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_RequesterId",
                table: "Friendship",
                newName: "IX_Friendship_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_AddresseeId",
                table: "Friendship",
                newName: "IX_Friendship_AddresseeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Friendship",
                table: "Friendship",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_Users_AddresseeId",
                table: "Friendship",
                column: "AddresseeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_Users_RequesterId",
                table: "Friendship",
                column: "RequesterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeonStrike2D.Backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreToGameResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "GameResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "GameResults");
        }
    }
}

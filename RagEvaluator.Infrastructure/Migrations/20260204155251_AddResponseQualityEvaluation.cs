using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseQualityEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasLanguageSwitching",
                table: "Queries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseQuality",
                table: "Queries",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasLanguageSwitching",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "ResponseQuality",
                table: "Queries");
        }
    }
}

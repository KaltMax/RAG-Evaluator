using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContentAndLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Documents");
        }
    }
}

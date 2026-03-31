using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMinChunkSizeToExperiment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinChunkSize",
                table: "Experiments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinChunkSize",
                table: "Experiments");
        }
    }
}

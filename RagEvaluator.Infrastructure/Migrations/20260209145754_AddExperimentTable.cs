using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentId",
                table: "Queries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepeatCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChunkingStrategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChatModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "integer", nullable: false),
                    SimilarityThreshold = table.Column<double>(type: "double precision", nullable: false),
                    PromptTemplate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalQueryCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedQueryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Queries_ExperimentId",
                table: "Queries",
                column: "ExperimentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Queries_Experiments_ExperimentId",
                table: "Queries",
                column: "ExperimentId",
                principalTable: "Experiments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Queries_Experiments_ExperimentId",
                table: "Queries");

            migrationBuilder.DropTable(
                name: "Experiments");

            migrationBuilder.DropIndex(
                name: "IX_Queries_ExperimentId",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "ExperimentId",
                table: "Queries");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryResultsAndExtendQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Answer",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChunkingStrategy",
                table: "Queries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "MRR",
                table: "Queries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NDCGAtK",
                table: "Queries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PrecisionAtK",
                table: "Queries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "QueryEmbedding",
                table: "Queries",
                type: "vector",
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "RecallAtK",
                table: "Queries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "Queries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QueryResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ChunkText = table.Column<string>(type: "text", nullable: false),
                    ChunkingStrategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    SimilarityScore = table.Column<double>(type: "double precision", nullable: false),
                    IsRelevant = table.Column<bool>(type: "boolean", nullable: true),
                    RelevanceGrade = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryResults_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_DocumentId",
                table: "QueryResults",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_QueryId",
                table: "QueryResults",
                column: "QueryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryResults");

            migrationBuilder.DropColumn(
                name: "Answer",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "ChunkingStrategy",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "MRR",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "NDCGAtK",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "PrecisionAtK",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "QueryEmbedding",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "RecallAtK",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "Queries");
        }
    }
}

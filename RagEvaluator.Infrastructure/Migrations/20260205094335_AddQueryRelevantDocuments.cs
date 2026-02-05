using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagEvaluator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryRelevantDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueryRelevantDocuments",
                columns: table => new
                {
                    QueryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryRelevantDocuments", x => new { x.QueryId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_QueryRelevantDocuments_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryRelevantDocuments_DocumentId",
                table: "QueryRelevantDocuments",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryRelevantDocuments");
        }
    }
}

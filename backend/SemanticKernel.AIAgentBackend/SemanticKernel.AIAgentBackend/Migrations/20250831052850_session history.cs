using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemanticKernel.AIAgentBackend.Migrations
{
    /// <inheritdoc />
    public partial class sessionhistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatHistory_SessionId",
                table: "ChatHistory");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SessionSummaries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_UserId",
                table: "SessionSummaries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_SessionId_UserId",
                table: "ChatHistory",
                columns: new[] { "SessionId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SessionSummaries_Users_UserId",
                table: "SessionSummaries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionSummaries_Users_UserId",
                table: "SessionSummaries");

            migrationBuilder.DropIndex(
                name: "IX_SessionSummaries_UserId",
                table: "SessionSummaries");

            migrationBuilder.DropIndex(
                name: "IX_ChatHistory_SessionId_UserId",
                table: "ChatHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SessionSummaries");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_SessionId",
                table: "ChatHistory",
                column: "SessionId");
        }
    }
}

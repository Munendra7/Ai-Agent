using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemanticKernel.AIAgentBackend.Migrations
{
    /// <inheritdoc />
    public partial class updateddataabsewithproperrelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ChatHistory",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_UserId",
                table: "ChatHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatHistory_SessionSummaries_SessionId",
                table: "ChatHistory",
                column: "SessionId",
                principalTable: "SessionSummaries",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatHistory_Users_UserId",
                table: "ChatHistory",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatHistory_SessionSummaries_SessionId",
                table: "ChatHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatHistory_Users_UserId",
                table: "ChatHistory");

            migrationBuilder.DropIndex(
                name: "IX_ChatHistory_UserId",
                table: "ChatHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatHistory");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries",
                column: "SessionId");
        }
    }
}

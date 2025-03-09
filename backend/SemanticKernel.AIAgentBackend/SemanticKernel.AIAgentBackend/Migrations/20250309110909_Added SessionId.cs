using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemanticKernel.AIAgentBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddedSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatHistory_UserId",
                table: "ChatHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatHistory");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "ChatHistory",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_SessionId",
                table: "ChatHistory",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatHistory_SessionId",
                table: "ChatHistory");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ChatHistory");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ChatHistory",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_UserId",
                table: "ChatHistory",
                column: "UserId");
        }
    }
}

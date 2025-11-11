using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP4952_Sockim.Migrations
{
    /// <inheritdoc />
    public partial class InvitationForChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation");

            migrationBuilder.AddColumn<int>(
                name: "ChatId",
                table: "ChatInvitation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation",
                columns: new[] { "SenderId", "ReceiverId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatInvitation_ChatId",
                table: "ChatInvitation",
                column: "ChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatInvitation_Chats_ChatId",
                table: "ChatInvitation",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatInvitation_Chats_ChatId",
                table: "ChatInvitation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation");

            migrationBuilder.DropIndex(
                name: "IX_ChatInvitation_ChatId",
                table: "ChatInvitation");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "ChatInvitation");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation",
                columns: new[] { "SenderId", "ReceiverId" });
        }
    }
}

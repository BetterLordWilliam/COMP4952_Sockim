using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP4952_Sockim.Migrations
{
    /// <inheritdoc />
    public partial class InvitationsListOnDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatInvitation_AspNetUsers_ReceiverId",
                table: "ChatInvitation");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatInvitation_AspNetUsers_SenderId",
                table: "ChatInvitation");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatInvitation_Chats_ChatId",
                table: "ChatInvitation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation");

            migrationBuilder.RenameTable(
                name: "ChatInvitation",
                newName: "Invitations");

            migrationBuilder.RenameIndex(
                name: "IX_ChatInvitation_ReceiverId",
                table: "Invitations",
                newName: "IX_Invitations_ReceiverId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatInvitation_ChatId",
                table: "Invitations",
                newName: "IX_Invitations_ChatId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invitations",
                table: "Invitations",
                columns: new[] { "SenderId", "ReceiverId", "ChatId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_AspNetUsers_ReceiverId",
                table: "Invitations",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_AspNetUsers_SenderId",
                table: "Invitations",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Chats_ChatId",
                table: "Invitations",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_AspNetUsers_ReceiverId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_AspNetUsers_SenderId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Chats_ChatId",
                table: "Invitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invitations",
                table: "Invitations");

            migrationBuilder.RenameTable(
                name: "Invitations",
                newName: "ChatInvitation");

            migrationBuilder.RenameIndex(
                name: "IX_Invitations_ReceiverId",
                table: "ChatInvitation",
                newName: "IX_ChatInvitation_ReceiverId");

            migrationBuilder.RenameIndex(
                name: "IX_Invitations_ChatId",
                table: "ChatInvitation",
                newName: "IX_ChatInvitation_ChatId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatInvitation",
                table: "ChatInvitation",
                columns: new[] { "SenderId", "ReceiverId", "ChatId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatInvitation_AspNetUsers_ReceiverId",
                table: "ChatInvitation",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatInvitation_AspNetUsers_SenderId",
                table: "ChatInvitation",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatInvitation_Chats_ChatId",
                table: "ChatInvitation",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

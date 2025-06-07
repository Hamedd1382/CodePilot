using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodePilot.Migrations
{
    public partial class Update_Models : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "Messages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "Chats",
                newName: "Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Chats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Chats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Chats");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Messages",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Chats",
                newName: "ChatId");
        }
    }
}

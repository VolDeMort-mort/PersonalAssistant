using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJournalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmotionTag",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "JournalEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "JournalEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LocalFilePath",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalText",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TelegramFileId",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscribedText",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "JournalEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "LocalFilePath",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "OriginalText",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "TelegramFileId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "TranscribedText",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "JournalEntries");

            migrationBuilder.AddColumn<string>(
                name: "EmotionTag",
                table: "JournalEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

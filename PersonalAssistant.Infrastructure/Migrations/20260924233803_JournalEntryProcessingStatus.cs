using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JournalEntryProcessingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "JournalEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JournalEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
        UPDATE JournalEntries
        SET Status = CASE
            WHEN Type = 0 THEN 0                      
            WHEN LocalFilePath IS NOT NULL THEN 2     
            ELSE 1                                    
        END");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "JournalEntries");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "JournalEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
        UPDATE JournalEntries
        SET IsProcessed = CASE WHEN Status IN (0, 3) THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "JournalEntries");
        }

    }
}

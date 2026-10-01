using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannerSettings",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    AgendaHour = table.Column<int>(type: "int", nullable: true),
                    LastAgendaSentOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerSettings", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    RepeatKind = table.Column<int>(type: "int", nullable: true),
                    RepeatInterval = table.Column<int>(type: "int", nullable: true),
                    RepeatDays = table.Column<int>(type: "int", nullable: true),
                    RepeatAnchorMonth = table.Column<int>(type: "int", nullable: true),
                    RepeatAnchorDay = table.Column<int>(type: "int", nullable: true),
                    RemindMinutesBefore = table.Column<int>(type: "int", nullable: true),
                    SnoozedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRemindedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannerTasks_PlannerTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "PlannerTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTasks_ChatId_CompletedAt_DueDate",
                table: "PlannerTasks",
                columns: new[] { "ChatId", "CompletedAt", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTasks_CompletedAt_DueDate",
                table: "PlannerTasks",
                columns: new[] { "CompletedAt", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTasks_TopicId",
                table: "PlannerTasks",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTopics_ChatId_Name",
                table: "PlannerTopics",
                columns: new[] { "ChatId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannerSettings");

            migrationBuilder.DropTable(
                name: "PlannerTasks");

            migrationBuilder.DropTable(
                name: "PlannerTopics");
        }
    }
}

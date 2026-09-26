using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PaidForDate",
                table: "FinanceTransactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledPaymentId",
                table: "FinanceTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recurrence = table.Column<int>(type: "int", nullable: false),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AnchorDay = table.Column<int>(type: "int", nullable: false),
                    RemindDaysBefore = table.Column<int>(type: "int", nullable: true),
                    RemindAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    LastRemindedFor = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_FinanceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FinanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTransactions_ScheduledPaymentId",
                table: "FinanceTransactions",
                column: "ScheduledPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_CategoryId",
                table: "ScheduledPayments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_ChatId_IsActive_NextDueDate",
                table: "ScheduledPayments",
                columns: new[] { "ChatId", "IsActive", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_IsActive_NextDueDate",
                table: "ScheduledPayments",
                columns: new[] { "IsActive", "NextDueDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceTransactions_ScheduledPayments_ScheduledPaymentId",
                table: "FinanceTransactions",
                column: "ScheduledPaymentId",
                principalTable: "ScheduledPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceTransactions_ScheduledPayments_ScheduledPaymentId",
                table: "FinanceTransactions");

            migrationBuilder.DropTable(
                name: "ScheduledPayments");

            migrationBuilder.DropIndex(
                name: "IX_FinanceTransactions_ScheduledPaymentId",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "PaidForDate",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "ScheduledPaymentId",
                table: "FinanceTransactions");
        }
    }
}

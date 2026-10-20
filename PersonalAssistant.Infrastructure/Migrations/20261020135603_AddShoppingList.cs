using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShoppingItemId",
                table: "FinanceTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShoppingItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BoughtAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingItems_FinanceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FinanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTransactions_ShoppingItemId",
                table: "FinanceTransactions",
                column: "ShoppingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_CategoryId",
                table: "ShoppingItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_ChatId_BoughtAt",
                table: "ShoppingItems",
                columns: new[] { "ChatId", "BoughtAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceTransactions_ShoppingItems_ShoppingItemId",
                table: "FinanceTransactions",
                column: "ShoppingItemId",
                principalTable: "ShoppingItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceTransactions_ShoppingItems_ShoppingItemId",
                table: "FinanceTransactions");

            migrationBuilder.DropTable(
                name: "ShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_FinanceTransactions_ShoppingItemId",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "ShoppingItemId",
                table: "FinanceTransactions");
        }
    }
}

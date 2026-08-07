using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientLinkToManualShoppingItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IngredientId",
                table: "ManualShoppingItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualShoppingItems_IngredientId",
                table: "ManualShoppingItems",
                column: "IngredientId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualShoppingItems_Ingredients_IngredientId",
                table: "ManualShoppingItems",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManualShoppingItems_Ingredients_IngredientId",
                table: "ManualShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_ManualShoppingItems_IngredientId",
                table: "ManualShoppingItems");

            migrationBuilder.DropColumn(
                name: "IngredientId",
                table: "ManualShoppingItems");
        }
    }
}

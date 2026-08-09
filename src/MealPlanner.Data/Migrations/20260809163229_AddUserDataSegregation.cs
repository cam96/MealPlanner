using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDataSegregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PantryItems_IngredientId_Location",
                table: "PantryItems");

            migrationBuilder.DropIndex(
                name: "IX_MealPlans_Year_Month",
                table: "MealPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManualShoppingItems_Year_Month",
                table: "ManualShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_GeneratedItemCartEntries_Year_Month_IngredientId",
                table: "GeneratedItemCartEntries");

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "People",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "PantryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "MealPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "ManualShoppingItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "GeneratedItemCartEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill existing records: assign orphaned data to the first user (by creation date).
            // If no users exist yet the tables are empty so this is a no-op.
            migrationBuilder.Sql("""
                UPDATE People SET AppUserId = (SELECT Id FROM AppUsers ORDER BY CreatedAt LIMIT 1)
                    WHERE AppUserId = 0 AND EXISTS (SELECT 1 FROM AppUsers);
                UPDATE PantryItems SET AppUserId = (SELECT Id FROM AppUsers ORDER BY CreatedAt LIMIT 1)
                    WHERE AppUserId = 0 AND EXISTS (SELECT 1 FROM AppUsers);
                UPDATE MealPlans SET AppUserId = (SELECT Id FROM AppUsers ORDER BY CreatedAt LIMIT 1)
                    WHERE AppUserId = 0 AND EXISTS (SELECT 1 FROM AppUsers);
                UPDATE ManualShoppingItems SET AppUserId = (SELECT Id FROM AppUsers ORDER BY CreatedAt LIMIT 1)
                    WHERE AppUserId = 0 AND EXISTS (SELECT 1 FROM AppUsers);
                UPDATE GeneratedItemCartEntries SET AppUserId = (SELECT Id FROM AppUsers ORDER BY CreatedAt LIMIT 1)
                    WHERE AppUserId = 0 AND EXISTS (SELECT 1 FROM AppUsers);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_People_AppUserId",
                table: "People",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_AppUserId_IngredientId_Location",
                table: "PantryItems",
                columns: new[] { "AppUserId", "IngredientId", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_IngredientId",
                table: "PantryItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_AppUserId_Year_Month",
                table: "MealPlans",
                columns: new[] { "AppUserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualShoppingItems_AppUserId_Year_Month",
                table: "ManualShoppingItems",
                columns: new[] { "AppUserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedItemCartEntries_AppUserId_Year_Month_IngredientId",
                table: "GeneratedItemCartEntries",
                columns: new[] { "AppUserId", "Year", "Month", "IngredientId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GeneratedItemCartEntries_AppUsers_AppUserId",
                table: "GeneratedItemCartEntries",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManualShoppingItems_AppUsers_AppUserId",
                table: "ManualShoppingItems",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlans_AppUsers_AppUserId",
                table: "MealPlans",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PantryItems_AppUsers_AppUserId",
                table: "PantryItems",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_People_AppUsers_AppUserId",
                table: "People",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GeneratedItemCartEntries_AppUsers_AppUserId",
                table: "GeneratedItemCartEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_ManualShoppingItems_AppUsers_AppUserId",
                table: "ManualShoppingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MealPlans_AppUsers_AppUserId",
                table: "MealPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_PantryItems_AppUsers_AppUserId",
                table: "PantryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_People_AppUsers_AppUserId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_AppUserId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_PantryItems_AppUserId_IngredientId_Location",
                table: "PantryItems");

            migrationBuilder.DropIndex(
                name: "IX_PantryItems_IngredientId",
                table: "PantryItems");

            migrationBuilder.DropIndex(
                name: "IX_MealPlans_AppUserId_Year_Month",
                table: "MealPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManualShoppingItems_AppUserId_Year_Month",
                table: "ManualShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_GeneratedItemCartEntries_AppUserId_Year_Month_IngredientId",
                table: "GeneratedItemCartEntries");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "PantryItems");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "ManualShoppingItems");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "GeneratedItemCartEntries");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_IngredientId_Location",
                table: "PantryItems",
                columns: new[] { "IngredientId", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_Year_Month",
                table: "MealPlans",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualShoppingItems_Year_Month",
                table: "ManualShoppingItems",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedItemCartEntries_Year_Month_IngredientId",
                table: "GeneratedItemCartEntries",
                columns: new[] { "Year", "Month", "IngredientId" },
                unique: true);
        }
    }
}

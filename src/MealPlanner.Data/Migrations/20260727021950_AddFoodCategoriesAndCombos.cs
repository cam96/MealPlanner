using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodCategoriesAndCombos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MealComboId",
                table: "PlannedMeals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Ingredients",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateTable(
                name: "MealCombos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProteinIngredientId = table.Column<int>(type: "INTEGER", nullable: true),
                    CarbohydrateIngredientId = table.Column<int>(type: "INTEGER", nullable: true),
                    VegetableIngredientId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealCombos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealCombos_Ingredients_CarbohydrateIngredientId",
                        column: x => x.CarbohydrateIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MealCombos_Ingredients_ProteinIngredientId",
                        column: x => x.ProteinIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MealCombos_Ingredients_VegetableIngredientId",
                        column: x => x.VegetableIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_MealComboId",
                table: "PlannedMeals",
                column: "MealComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Category",
                table: "Ingredients",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MealCombos_CarbohydrateIngredientId",
                table: "MealCombos",
                column: "CarbohydrateIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealCombos_ProteinIngredientId",
                table: "MealCombos",
                column: "ProteinIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealCombos_VegetableIngredientId",
                table: "MealCombos",
                column: "VegetableIngredientId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMeals_MealCombos_MealComboId",
                table: "PlannedMeals",
                column: "MealComboId",
                principalTable: "MealCombos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMeals_MealCombos_MealComboId",
                table: "PlannedMeals");

            migrationBuilder.DropTable(
                name: "MealCombos");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMeals_MealComboId",
                table: "PlannedMeals");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_Category",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "MealComboId",
                table: "PlannedMeals");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Ingredients");
        }
    }
}

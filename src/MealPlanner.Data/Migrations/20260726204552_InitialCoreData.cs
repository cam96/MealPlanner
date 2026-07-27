using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MealPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BaseUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CaloriesPer100 = table.Column<double>(type: "REAL", nullable: false),
                    ProteinPer100 = table.Column<double>(type: "REAL", nullable: false),
                    FiberPer100 = table.Column<double>(type: "REAL", nullable: false),
                    IsNutritionEstimated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CnfFoodCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ServingWeightG = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DailyCalorieGoal = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyProteinGoal = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyFiberGoal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PackageQuantity = table.Column<double>(type: "REAL", nullable: false),
                    PackageUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsEstimated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPreferredStore = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientPrices_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientPrices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Stores",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Costco" },
                    { 2, "Superstore" },
                    { 3, "Safeway" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientPrices_IngredientId_StoreId_RecordedDate",
                table: "IngredientPrices",
                columns: new[] { "IngredientId", "StoreId", "RecordedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientPrices_StoreId",
                table: "IngredientPrices",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Name",
                table: "Ingredients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Name",
                table: "Stores",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientPrices");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "Stores");
        }
    }
}

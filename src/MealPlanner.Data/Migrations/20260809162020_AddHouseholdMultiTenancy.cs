using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MealPlanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_Name",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Name",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_MealPlans_Year_Month",
                table: "MealPlans");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_Category",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_Name",
                table: "Ingredients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppSettings",
                table: "AppSettings");

            migrationBuilder.DeleteData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "Stores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "People",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "PantryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "MealPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "MealCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "ManualShoppingItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "Ingredients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "IngredientPrices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "AppUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HouseholdId",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppSettings",
                table: "AppSettings",
                columns: new[] { "HouseholdId", "Key" });

            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Households_AppUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseholdId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AcceptedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdInvites_AppUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HouseholdInvites_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdInvites_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_HouseholdId_Name",
                table: "Stores",
                columns: new[] { "HouseholdId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_HouseholdId_Name",
                table: "Recipes",
                columns: new[] { "HouseholdId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_People_HouseholdId",
                table: "People",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_HouseholdId",
                table: "PantryItems",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_HouseholdId_Year_Month",
                table: "MealPlans",
                columns: new[] { "HouseholdId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealCombos_HouseholdId",
                table: "MealCombos",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualShoppingItems_HouseholdId",
                table: "ManualShoppingItems",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_HouseholdId_Category",
                table: "Ingredients",
                columns: new[] { "HouseholdId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_HouseholdId_Name",
                table: "Ingredients",
                columns: new[] { "HouseholdId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientPrices_HouseholdId",
                table: "IngredientPrices",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_HouseholdId",
                table: "AppUsers",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvites_AcceptedByUserId",
                table: "HouseholdInvites",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvites_CreatedByUserId",
                table: "HouseholdInvites",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvites_HouseholdId",
                table: "HouseholdInvites",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvites_Token",
                table: "HouseholdInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Households_OwnerId",
                table: "Households",
                column: "OwnerId");

            // --- Data migration: create a default household for existing data ---
            migrationBuilder.Sql("""
                INSERT INTO Households (Name, OwnerId, CreatedAt)
                SELECT 'My Household', Id, datetime('now')
                FROM AppUsers
                ORDER BY CreatedAt
                LIMIT 1;
                """);

            migrationBuilder.Sql("""
                UPDATE Stores SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE Recipes SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE People SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE PantryItems SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE MealPlans SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE MealCombos SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE ManualShoppingItems SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE Ingredients SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE IngredientPrices SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE AppSettings SET HouseholdId = COALESCE((SELECT Id FROM Households LIMIT 1), 1) WHERE HouseholdId = 0;
                UPDATE AppUsers SET HouseholdId = (SELECT Id FROM Households LIMIT 1) WHERE HouseholdId IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSettings_Households_HouseholdId",
                table: "AppSettings",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Households_HouseholdId",
                table: "AppUsers",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IngredientPrices_Households_HouseholdId",
                table: "IngredientPrices",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_Households_HouseholdId",
                table: "Ingredients",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManualShoppingItems_Households_HouseholdId",
                table: "ManualShoppingItems",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealCombos_Households_HouseholdId",
                table: "MealCombos",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlans_Households_HouseholdId",
                table: "MealPlans",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PantryItems_Households_HouseholdId",
                table: "PantryItems",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Households_HouseholdId",
                table: "People",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Households_HouseholdId",
                table: "Recipes",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_Households_HouseholdId",
                table: "Stores",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppSettings_Households_HouseholdId",
                table: "AppSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Households_HouseholdId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_IngredientPrices_Households_HouseholdId",
                table: "IngredientPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_Households_HouseholdId",
                table: "Ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_ManualShoppingItems_Households_HouseholdId",
                table: "ManualShoppingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MealCombos_Households_HouseholdId",
                table: "MealCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_MealPlans_Households_HouseholdId",
                table: "MealPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_PantryItems_Households_HouseholdId",
                table: "PantryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Households_HouseholdId",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Households_HouseholdId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Stores_Households_HouseholdId",
                table: "Stores");

            migrationBuilder.DropTable(
                name: "HouseholdInvites");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropIndex(
                name: "IX_Stores_HouseholdId_Name",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_HouseholdId_Name",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_People_HouseholdId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_PantryItems_HouseholdId",
                table: "PantryItems");

            migrationBuilder.DropIndex(
                name: "IX_MealPlans_HouseholdId_Year_Month",
                table: "MealPlans");

            migrationBuilder.DropIndex(
                name: "IX_MealCombos_HouseholdId",
                table: "MealCombos");

            migrationBuilder.DropIndex(
                name: "IX_ManualShoppingItems_HouseholdId",
                table: "ManualShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_HouseholdId_Category",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_HouseholdId_Name",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_IngredientPrices_HouseholdId",
                table: "IngredientPrices");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_HouseholdId",
                table: "AppUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppSettings",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "PantryItems");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "MealCombos");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "ManualShoppingItems");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "IngredientPrices");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "AppSettings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppSettings",
                table: "AppSettings",
                column: "Key");

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
                name: "IX_Stores_Name",
                table: "Stores",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                table: "Recipes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_Year_Month",
                table: "MealPlans",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Category",
                table: "Ingredients",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_Name",
                table: "Ingredients",
                column: "Name");
        }
    }
}

using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="RecipeIngredient"/>.</summary>
public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.Unit)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Deleting an ingredient still referenced by a recipe should be blocked rather than
        // silently removing recipe lines.
        builder.HasOne(ri => ri.Ingredient)
            .WithMany()
            .HasForeignKey(ri => ri.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ri => new { ri.RecipeId, ri.IngredientId });
    }
}

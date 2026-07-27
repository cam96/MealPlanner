using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="Recipe"/>.</summary>
public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(r => r.Name);

        builder.Property(r => r.MealType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Instructions)
            .HasMaxLength(4000);

        builder.HasMany(r => r.Ingredients)
            .WithOne(ri => ri.Recipe)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

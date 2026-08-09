using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="Ingredient"/>.</summary>
public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(i => new { i.HouseholdId, i.Name });

        builder.Property(i => i.BaseUnit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Category)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(i => new { i.HouseholdId, i.Category });

        builder.HasOne(i => i.Household)
            .WithMany()
            .HasForeignKey(i => i.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Prices)
            .WithOne(p => p.Ingredient)
            .HasForeignKey(p => p.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

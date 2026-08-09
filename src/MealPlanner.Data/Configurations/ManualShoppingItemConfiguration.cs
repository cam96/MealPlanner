using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="ManualShoppingItem"/>.</summary>
public sealed class ManualShoppingItemConfiguration : IEntityTypeConfiguration<ManualShoppingItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ManualShoppingItem> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Household)
            .WithMany()
            .HasForeignKey(m => m.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Unit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(m => m.Ingredient)
            .WithMany()
            .HasForeignKey(m => m.IngredientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.Year, m.Month });
    }
}

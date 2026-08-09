using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="PantryItem"/>.</summary>
public sealed class PantryItemConfiguration : IEntityTypeConfiguration<PantryItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PantryItem> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Unit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Location)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(p => p.AppUser)
            .WithMany(u => u.PantryItems)
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting an ingredient still stocked in the pantry should be blocked rather than silently
        // removing inventory records.
        builder.HasOne(p => p.Ingredient)
            .WithMany()
            .HasForeignKey(p => p.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.AppUserId, p.IngredientId, p.Location });
    }
}

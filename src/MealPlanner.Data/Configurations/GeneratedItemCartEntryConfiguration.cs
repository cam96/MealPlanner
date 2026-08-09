using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="GeneratedItemCartEntry"/>.</summary>
public sealed class GeneratedItemCartEntryConfiguration : IEntityTypeConfiguration<GeneratedItemCartEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneratedItemCartEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.AppUser)
            .WithMany(u => u.GeneratedItemCartEntries)
            .HasForeignKey(e => e.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Ingredient)
            .WithMany()
            .HasForeignKey(e => e.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Each ingredient can only be carted once per user per shopping period.
        builder.HasIndex(e => new { e.AppUserId, e.Year, e.Month, e.IngredientId })
            .IsUnique();
    }
}

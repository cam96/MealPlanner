using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="PlannedMeal"/>.</summary>
public sealed class PlannedMealConfiguration : IEntityTypeConfiguration<PlannedMeal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlannedMeal> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Slot)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Assignee)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Deleting a recipe that is still planned should be blocked rather than silently emptying
        // the slot.
        builder.HasOne(m => m.Recipe)
            .WithMany()
            .HasForeignKey(m => m.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Likewise, deleting a combo that is still planned should be blocked.
        builder.HasOne(m => m.MealCombo)
            .WithMany()
            .HasForeignKey(m => m.MealComboId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.DayPlanId, m.Slot });
    }
}

using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="DayPlan"/>.</summary>
public sealed class DayPlanConfiguration : IEntityTypeConfiguration<DayPlan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DayPlan> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DayType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.Note)
            .HasMaxLength(500);

        // One day plan per date within a month.
        builder.HasIndex(d => new { d.MealPlanId, d.Date }).IsUnique();

        builder.HasMany(d => d.Meals)
            .WithOne(m => m.DayPlan)
            .HasForeignKey(m => m.DayPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

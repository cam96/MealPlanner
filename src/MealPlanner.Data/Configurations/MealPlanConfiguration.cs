using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="MealPlan"/>.</summary>
public sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.HasKey(p => p.Id);

        // One plan per household per calendar month.
        builder.HasIndex(p => new { p.HouseholdId, p.Year, p.Month }).IsUnique();

        builder.HasOne(p => p.Household)
            .WithMany()
            .HasForeignKey(p => p.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Days)
            .WithOne(d => d.MealPlan)
            .HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

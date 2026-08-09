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

        builder.HasOne(p => p.AppUser)
            .WithMany(u => u.MealPlans)
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One plan per user per calendar month.
        builder.HasIndex(p => new { p.AppUserId, p.Year, p.Month }).IsUnique();

        builder.HasMany(p => p.Days)
            .WithOne(d => d.MealPlan)
            .HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

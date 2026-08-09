using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="Store"/>, including seed data for the household's stores.</summary>
public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => new { s.HouseholdId, s.Name }).IsUnique();

        builder.HasOne(s => s.Household)
            .WithMany()
            .HasForeignKey(s => s.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

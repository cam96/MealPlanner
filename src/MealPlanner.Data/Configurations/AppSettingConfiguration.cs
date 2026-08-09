using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="AppSetting"/>.</summary>
public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(s => new { s.HouseholdId, s.Key });

        builder.Property(s => s.Key).HasMaxLength(100);
        builder.Property(s => s.Value).HasMaxLength(500);

        builder.HasOne(s => s.Household)
            .WithMany()
            .HasForeignKey(s => s.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="AppUserRole"/>.</summary>
public sealed class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppUserRole> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Role).IsRequired().HasMaxLength(50);

        // Prevent duplicate role assignments for the same user.
        builder.HasIndex(r => new { r.AppUserId, r.Role }).IsUnique();
    }
}

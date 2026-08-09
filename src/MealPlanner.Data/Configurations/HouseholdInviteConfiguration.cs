using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="HouseholdInvite"/>.</summary>
public sealed class HouseholdInviteConfiguration : IEntityTypeConfiguration<HouseholdInvite>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdInvite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Token).IsRequired().HasMaxLength(64);
        builder.HasIndex(i => i.Token).IsUnique();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AcceptedByUser)
            .WithMany()
            .HasForeignKey(i => i.AcceptedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="IngredientPrice"/>.</summary>
public sealed class IngredientPriceConfiguration : IEntityTypeConfiguration<IngredientPrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IngredientPrice> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Price)
            .HasColumnType("TEXT")
            .HasPrecision(10, 2);

        builder.Property(p => p.PackageUnit)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(p => p.Store)
            .WithMany(s => s.Prices)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.IngredientId, p.StoreId, p.RecordedDate });
    }
}

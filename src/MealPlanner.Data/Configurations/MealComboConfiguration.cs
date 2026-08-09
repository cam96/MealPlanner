using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPlanner.Data.Configurations;

/// <summary>EF Core configuration for <see cref="MealCombo"/>.</summary>
public sealed class MealComboConfiguration : IEntityTypeConfiguration<MealCombo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MealCombo> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(c => c.Household)
            .WithMany()
            .HasForeignKey(c => c.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.HouseholdId);

        // A combo references interchangeable ingredients. Deleting an ingredient that a combo points
        // at should clear the slot rather than block the delete, since combos are informal.
        builder.HasOne(c => c.ProteinIngredient)
            .WithMany()
            .HasForeignKey(c => c.ProteinIngredientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.CarbohydrateIngredient)
            .WithMany()
            .HasForeignKey(c => c.CarbohydrateIngredientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.VegetableIngredient)
            .WithMany()
            .HasForeignKey(c => c.VegetableIngredientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

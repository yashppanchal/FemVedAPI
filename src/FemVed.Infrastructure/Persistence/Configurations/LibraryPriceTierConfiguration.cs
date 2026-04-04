using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryPriceTier"/> → <c>library_price_tiers</c>.</summary>
internal sealed class LibraryPriceTierConfiguration : IEntityTypeConfiguration<LibraryPriceTier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryPriceTier> builder)
    {
        builder.ToTable("library_price_tiers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.TierKey).HasColumnName("tier_key").HasMaxLength(20).IsRequired();
        builder.Property(t => t.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(t => t.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(t => t.TierKey).IsUnique().HasDatabaseName("uq_library_price_tiers_tier_key");

        // Seed 4 tiers
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new LibraryPriceTier { Id = Guid.Parse("44444444-0000-0000-0000-000000000001"), TierKey = "MOVIE", DisplayName = "Movie", SortOrder = 1, IsActive = true, CreatedAt = ts },
            new LibraryPriceTier { Id = Guid.Parse("44444444-0000-0000-0000-000000000002"), TierKey = "STANDARD", DisplayName = "Standard", SortOrder = 2, IsActive = true, CreatedAt = ts },
            new LibraryPriceTier { Id = Guid.Parse("44444444-0000-0000-0000-000000000003"), TierKey = "MEDIUM", DisplayName = "Medium", SortOrder = 3, IsActive = true, CreatedAt = ts },
            new LibraryPriceTier { Id = Guid.Parse("44444444-0000-0000-0000-000000000004"), TierKey = "LARGE", DisplayName = "Large", SortOrder = 4, IsActive = true, CreatedAt = ts }
        );
    }
}

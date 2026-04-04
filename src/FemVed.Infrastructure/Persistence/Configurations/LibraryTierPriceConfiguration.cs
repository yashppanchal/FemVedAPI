using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryTierPrice"/> → <c>library_tier_prices</c>.</summary>
internal sealed class LibraryTierPriceConfiguration : IEntityTypeConfiguration<LibraryTierPrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryTierPrice> builder)
    {
        builder.ToTable("library_tier_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.TierId).HasColumnName("tier_id").IsRequired();
        builder.Property(p => p.LocationCode).HasColumnName("location_code").HasMaxLength(5).IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(p => p.CurrencySymbol).HasColumnName("currency_symbol").HasMaxLength(5).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => new { p.TierId, p.LocationCode }).IsUnique().HasDatabaseName("uq_library_tier_prices_tier_location");

        builder.HasOne(p => p.Tier)
            .WithMany(t => t.Prices)
            .HasForeignKey(p => p.TierId)
            .HasConstraintName("fk_library_tier_prices_tier");

        // Seed tier prices for all 4 tiers × 3 regions
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var movieId = Guid.Parse("44444444-0000-0000-0000-000000000001");
        var standardId = Guid.Parse("44444444-0000-0000-0000-000000000002");
        var mediumId = Guid.Parse("44444444-0000-0000-0000-000000000003");
        var largeId = Guid.Parse("44444444-0000-0000-0000-000000000004");

        builder.HasData(
            // Movie tier
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0001-000000000001"), TierId = movieId, LocationCode = "IN", Amount = 499m, CurrencyCode = "INR", CurrencySymbol = "₹", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0001-000000000002"), TierId = movieId, LocationCode = "GB", Amount = 9m, CurrencyCode = "GBP", CurrencySymbol = "£", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0001-000000000003"), TierId = movieId, LocationCode = "US", Amount = 12m, CurrencyCode = "USD", CurrencySymbol = "$", CreatedAt = ts, UpdatedAt = ts },
            // Standard tier
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0002-000000000001"), TierId = standardId, LocationCode = "IN", Amount = 999m, CurrencyCode = "INR", CurrencySymbol = "₹", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0002-000000000002"), TierId = standardId, LocationCode = "GB", Amount = 19m, CurrencyCode = "GBP", CurrencySymbol = "£", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0002-000000000003"), TierId = standardId, LocationCode = "US", Amount = 24m, CurrencyCode = "USD", CurrencySymbol = "$", CreatedAt = ts, UpdatedAt = ts },
            // Medium tier
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0003-000000000001"), TierId = mediumId, LocationCode = "IN", Amount = 1499m, CurrencyCode = "INR", CurrencySymbol = "₹", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0003-000000000002"), TierId = mediumId, LocationCode = "GB", Amount = 29m, CurrencyCode = "GBP", CurrencySymbol = "£", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0003-000000000003"), TierId = mediumId, LocationCode = "US", Amount = 35m, CurrencyCode = "USD", CurrencySymbol = "$", CreatedAt = ts, UpdatedAt = ts },
            // Large tier
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0004-000000000001"), TierId = largeId, LocationCode = "IN", Amount = 2199m, CurrencyCode = "INR", CurrencySymbol = "₹", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0004-000000000002"), TierId = largeId, LocationCode = "GB", Amount = 39m, CurrencyCode = "GBP", CurrencySymbol = "£", CreatedAt = ts, UpdatedAt = ts },
            new LibraryTierPrice { Id = Guid.Parse("55555555-0000-0000-0004-000000000003"), TierId = largeId, LocationCode = "US", Amount = 49m, CurrencyCode = "USD", CurrencySymbol = "$", CreatedAt = ts, UpdatedAt = ts }
        );
    }
}

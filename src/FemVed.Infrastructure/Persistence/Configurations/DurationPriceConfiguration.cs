using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="DurationPrice"/> → <c>duration_prices</c>.</summary>
internal sealed class DurationPriceConfiguration : IEntityTypeConfiguration<DurationPrice>
{
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DurationPrice> builder)
    {
        builder.ToTable("duration_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.DurationId).HasColumnName("duration_id").IsRequired();
        builder.Property(p => p.LocationCode).HasColumnName("location_code").HasMaxLength(5).IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(p => p.CurrencySymbol).HasColumnName("currency_symbol").HasMaxLength(5).IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => new { p.DurationId, p.LocationCode })
            .IsUnique()
            .HasDatabaseName("uq_duration_prices_duration_location");
        builder.HasIndex(p => new { p.DurationId, p.LocationCode })
            .HasDatabaseName("idx_duration_prices_duration_location");

        builder.HasOne(p => p.Duration)
            .WithMany(d => d.Prices)
            .HasForeignKey(p => p.DurationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_duration_prices_duration");

        var d1 = Guid.Parse("66666666-0000-0000-0000-000000000001");
        var d2 = Guid.Parse("66666666-0000-0000-0000-000000000002");
        var d3 = Guid.Parse("66666666-0000-0000-0000-000000000003");
        var d4 = Guid.Parse("66666666-0000-0000-0000-000000000004");
        var d5 = Guid.Parse("66666666-0000-0000-0000-000000000005");

        builder.HasData(
            // Duration 1: 6 weeks
            new DurationPrice { Id = Guid.Parse("77777777-0001-0001-0000-000000000001"), DurationId = d1, LocationCode = "US", Amount = 400.00m, CurrencyCode = "USD", CurrencySymbol = "$", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0001-0002-0000-000000000001"), DurationId = d1, LocationCode = "GB", Amount = 320.00m, CurrencyCode = "GBP", CurrencySymbol = "£", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0001-0003-0000-000000000001"), DurationId = d1, LocationCode = "IN", Amount = 33000.00m, CurrencyCode = "INR", CurrencySymbol = "₹", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            // Duration 2: 4 weeks
            new DurationPrice { Id = Guid.Parse("77777777-0002-0001-0000-000000000001"), DurationId = d2, LocationCode = "US", Amount = 350.00m, CurrencyCode = "USD", CurrencySymbol = "$", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0002-0002-0000-000000000001"), DurationId = d2, LocationCode = "GB", Amount = 280.00m, CurrencyCode = "GBP", CurrencySymbol = "£", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0002-0003-0000-000000000001"), DurationId = d2, LocationCode = "IN", Amount = 29000.00m, CurrencyCode = "INR", CurrencySymbol = "₹", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            // Duration 3: 8 weeks (Kimberly PCOS)
            new DurationPrice { Id = Guid.Parse("77777777-0003-0001-0000-000000000001"), DurationId = d3, LocationCode = "GB", Amount = 879.00m, CurrencyCode = "GBP", CurrencySymbol = "£", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0003-0002-0000-000000000001"), DurationId = d3, LocationCode = "US", Amount = 1099.00m, CurrencyCode = "USD", CurrencySymbol = "$", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0003-0003-0000-000000000001"), DurationId = d3, LocationCode = "IN", Amount = 90000.00m, CurrencyCode = "INR", CurrencySymbol = "₹", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            // Duration 4: 4 weeks (Kimberly Metabolism)
            new DurationPrice { Id = Guid.Parse("77777777-0004-0001-0000-000000000001"), DurationId = d4, LocationCode = "GB", Amount = 499.00m, CurrencyCode = "GBP", CurrencySymbol = "£", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0004-0002-0000-000000000001"), DurationId = d4, LocationCode = "US", Amount = 625.00m, CurrencyCode = "USD", CurrencySymbol = "$", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0004-0003-0000-000000000001"), DurationId = d4, LocationCode = "IN", Amount = 51000.00m, CurrencyCode = "INR", CurrencySymbol = "₹", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            // Duration 5: 8 weeks (Happy Hormone)
            new DurationPrice { Id = Guid.Parse("77777777-0005-0001-0000-000000000001"), DurationId = d5, LocationCode = "GB", Amount = 899.00m, CurrencyCode = "GBP", CurrencySymbol = "£", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0005-0002-0000-000000000001"), DurationId = d5, LocationCode = "US", Amount = 1125.00m, CurrencyCode = "USD", CurrencySymbol = "$", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new DurationPrice { Id = Guid.Parse("77777777-0005-0003-0000-000000000001"), DurationId = d5, LocationCode = "IN", Amount = 92000.00m, CurrencyCode = "INR", CurrencySymbol = "₹", IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded }
        );
    }
}

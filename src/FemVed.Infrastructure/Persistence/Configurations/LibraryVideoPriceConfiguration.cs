using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideoPrice"/> → <c>library_video_prices</c>.</summary>
internal sealed class LibraryVideoPriceConfiguration : IEntityTypeConfiguration<LibraryVideoPrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideoPrice> builder)
    {
        builder.ToTable("library_video_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(p => p.LocationCode).HasColumnName("location_code").HasMaxLength(5).IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(p => p.CurrencySymbol).HasColumnName("currency_symbol").HasMaxLength(5).IsRequired();
        builder.Property(p => p.OriginalAmount).HasColumnName("original_amount").HasColumnType("decimal(12,2)");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => new { p.VideoId, p.LocationCode }).IsUnique().HasDatabaseName("uq_library_video_prices_video_location");

        builder.HasOne(p => p.Video)
            .WithMany(v => v.PriceOverrides)
            .HasForeignKey(p => p.VideoId)
            .HasConstraintName("fk_library_video_prices_video");
    }
}

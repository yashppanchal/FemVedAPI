using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="Coupon"/> → <c>coupons</c>.</summary>
internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(c => c.DiscountType)
            .HasColumnName("discount_type")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<DiscountType>(v, true));
        builder.Property(c => c.DiscountValue).HasColumnName("discount_value").HasColumnType("decimal(10,2)").IsRequired();
        builder.Property(c => c.MinOrderAmount).HasColumnName("min_order_amount").HasColumnType("decimal(12,2)");
        builder.Property(c => c.MaxUses).HasColumnName("max_uses");
        builder.Property(c => c.UsedCount).HasColumnName("used_count").HasDefaultValue(0);
        builder.Property(c => c.ValidFrom).HasColumnName("valid_from");
        builder.Property(c => c.ValidUntil).HasColumnName("valid_until");
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("uq_coupons_code");
    }
}

using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="Order"/> → <c>orders</c>.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(o => o.DurationId).HasColumnName("duration_id").IsRequired();
        builder.Property(o => o.DurationPriceId).HasColumnName("duration_price_id").IsRequired();
        builder.Property(o => o.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(o => o.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(o => o.LocationCode).HasColumnName("location_code").HasMaxLength(5).IsRequired();
        builder.Property(o => o.CouponId).HasColumnName("coupon_id");
        builder.Property(o => o.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<OrderStatus>(v, true));
        builder.Property(o => o.PaymentGateway)
            .HasColumnName("payment_gateway")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<PaymentGateway>(v, true));
        builder.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(o => o.GatewayOrderId).HasColumnName("gateway_order_id").HasMaxLength(200);
        builder.Property(o => o.GatewayPaymentId).HasColumnName("gateway_payment_id").HasMaxLength(200);
        builder.Property(o => o.GatewayResponse).HasColumnName("gateway_response");
        builder.Property(o => o.FailureReason).HasColumnName("failure_reason");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(o => o.IdempotencyKey).IsUnique().HasDatabaseName("uq_orders_idempotency_key");
        builder.HasIndex(o => o.UserId).HasDatabaseName("idx_orders_user_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("idx_orders_status");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("idx_orders_created_at");

        builder.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).HasConstraintName("fk_orders_user");
        builder.HasOne(o => o.Duration).WithMany().HasForeignKey(o => o.DurationId).HasConstraintName("fk_orders_duration");
        builder.HasOne(o => o.DurationPrice).WithMany().HasForeignKey(o => o.DurationPriceId).HasConstraintName("fk_orders_duration_price");
        builder.HasOne(o => o.Coupon).WithMany().HasForeignKey(o => o.CouponId).HasConstraintName("fk_orders_coupon");
    }
}

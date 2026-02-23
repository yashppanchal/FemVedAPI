using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="Refund"/> → <c>refunds</c>.</summary>
internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(r => r.RefundAmount).HasColumnName("refund_amount").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(r => r.Reason).HasColumnName("reason");
        builder.Property(r => r.GatewayRefundId).HasColumnName("gateway_refund_id").HasMaxLength(200);
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<RefundStatus>(v, true));
        builder.Property(r => r.InitiatedBy).HasColumnName("initiated_by").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(r => r.OrderId).HasDatabaseName("idx_refunds_order_id");

        builder.HasOne(r => r.Order).WithMany(o => o.Refunds).HasForeignKey(r => r.OrderId).HasConstraintName("fk_refunds_order");
        builder.HasOne(r => r.InitiatedByUser).WithMany().HasForeignKey(r => r.InitiatedBy).HasConstraintName("fk_refunds_initiated_by");
    }
}

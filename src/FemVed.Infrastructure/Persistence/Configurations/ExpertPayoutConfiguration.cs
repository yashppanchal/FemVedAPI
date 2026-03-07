using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ExpertPayout"/> → <c>expert_payouts</c>.</summary>
internal sealed class ExpertPayoutConfiguration : IEntityTypeConfiguration<ExpertPayout>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpertPayout> builder)
    {
        builder.ToTable("expert_payouts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.ExpertId).HasColumnName("expert_id").IsRequired();
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(e => e.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.PaidBy).HasColumnName("paid_by").IsRequired();
        builder.Property(e => e.PaidAt).HasColumnName("paid_at").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(e => e.ExpertId).HasDatabaseName("idx_expert_payouts_expert_id");
        builder.HasIndex(e => e.PaidAt).HasDatabaseName("idx_expert_payouts_paid_at");

        builder.HasOne(e => e.Expert)
            .WithMany()
            .HasForeignKey(e => e.ExpertId)
            .HasConstraintName("fk_expert_payouts_expert")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PaidByUser)
            .WithMany()
            .HasForeignKey(e => e.PaidBy)
            .HasConstraintName("fk_expert_payouts_paid_by")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

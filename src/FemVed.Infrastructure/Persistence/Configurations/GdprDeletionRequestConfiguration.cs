using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="GdprDeletionRequest"/> → <c>gdpr_deletion_requests</c>.</summary>
internal sealed class GdprDeletionRequestConfiguration : IEntityTypeConfiguration<GdprDeletionRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GdprDeletionRequest> builder)
    {
        builder.ToTable("gdpr_deletion_requests");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(g => g.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(g => g.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("NOW()");
        builder.Property(g => g.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<GdprRequestStatus>(v, true));
        builder.Property(g => g.CompletedAt).HasColumnName("completed_at");
        builder.Property(g => g.RejectionReason).HasColumnName("rejection_reason");
        builder.Property(g => g.ProcessedBy).HasColumnName("processed_by");

        builder.HasIndex(g => g.UserId).HasDatabaseName("idx_gdpr_deletion_requests_user_id");
        builder.HasIndex(g => g.Status).HasDatabaseName("idx_gdpr_deletion_requests_status");

        builder.HasOne(g => g.User).WithMany().HasForeignKey(g => g.UserId).HasConstraintName("fk_gdpr_user");
        builder.HasOne(g => g.ProcessedByUser).WithMany().HasForeignKey(g => g.ProcessedBy).HasConstraintName("fk_gdpr_processed_by");
    }
}

using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ExpertProgressUpdate"/> → <c>expert_progress_updates</c>.</summary>
internal sealed class ExpertProgressUpdateConfiguration : IEntityTypeConfiguration<ExpertProgressUpdate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpertProgressUpdate> builder)
    {
        builder.ToTable("expert_progress_updates");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.AccessId).HasColumnName("access_id").IsRequired();
        builder.Property(u => u.ExpertId).HasColumnName("expert_id").IsRequired();
        builder.Property(u => u.UpdateNote).HasColumnName("update_note").IsRequired();
        builder.Property(u => u.SendEmail).HasColumnName("send_email").HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(u => u.AccessId).HasDatabaseName("idx_epu_access_id");

        builder.HasOne(u => u.Access).WithMany(a => a.ProgressUpdates).HasForeignKey(u => u.AccessId).HasConstraintName("fk_epu_access");
        builder.HasOne(u => u.Expert).WithMany().HasForeignKey(u => u.ExpertId).HasConstraintName("fk_epu_expert");
    }
}

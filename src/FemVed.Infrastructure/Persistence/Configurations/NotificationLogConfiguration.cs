using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="NotificationLog"/> → <c>notification_log</c>.</summary>
internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_log");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(n => n.UserId).HasColumnName("user_id");
        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<NotificationType>(v, true));
        builder.Property(n => n.TemplateKey).HasColumnName("template_key").HasMaxLength(100).IsRequired();
        builder.Property(n => n.Recipient).HasColumnName("recipient").HasMaxLength(300).IsRequired();
        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<NotificationStatus>(v, true));
        builder.Property(n => n.ErrorMessage).HasColumnName("error_message");
        builder.Property(n => n.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(n => n.UserId).HasDatabaseName("idx_notification_log_user_id");
        builder.HasIndex(n => n.Type).HasDatabaseName("idx_notification_log_type");

        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).HasConstraintName("fk_notification_log_user");
    }
}

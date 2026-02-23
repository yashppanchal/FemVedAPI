using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="AdminAuditLog"/> → <c>admin_audit_log</c>.</summary>
internal sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_log");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.AdminUserId).HasColumnName("admin_user_id").IsRequired();
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id");
        builder.Property(a => a.BeforeValue).HasColumnName("before_value").HasColumnType("jsonb");
        builder.Property(a => a.AfterValue).HasColumnName("after_value").HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => a.AdminUserId).HasDatabaseName("idx_admin_audit_log_admin_user_id");
        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("idx_admin_audit_log_entity_type_entity_id");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("idx_admin_audit_log_created_at");

        builder.HasOne(a => a.AdminUser).WithMany().HasForeignKey(a => a.AdminUserId).HasConstraintName("fk_admin_audit_log_user");
    }
}

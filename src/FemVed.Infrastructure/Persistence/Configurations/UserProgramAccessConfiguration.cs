using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="UserProgramAccess"/> → <c>user_program_access</c>.</summary>
internal sealed class UserProgramAccessConfiguration : IEntityTypeConfiguration<UserProgramAccess>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserProgramAccess> builder)
    {
        builder.ToTable("user_program_access");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(a => a.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(a => a.DurationId).HasColumnName("duration_id").IsRequired();
        builder.Property(a => a.ExpertId).HasColumnName("expert_id").IsRequired();
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<UserProgramAccessStatus>(v, true));
        builder.Property(a => a.ReminderSent).HasColumnName("reminder_sent").HasDefaultValue(false);
        builder.Property(a => a.StartedAt).HasColumnName("started_at");
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => a.UserId).HasDatabaseName("idx_upa_user_id");
        builder.HasIndex(a => a.ProgramId).HasDatabaseName("idx_upa_program_id");
        builder.HasIndex(a => a.ExpertId).HasDatabaseName("idx_upa_expert_id");
        builder.HasIndex(a => a.Status).HasDatabaseName("idx_upa_status");

        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).HasConstraintName("fk_upa_user");
        builder.HasOne(a => a.Order).WithMany(o => o.ProgramAccesses).HasForeignKey(a => a.OrderId).HasConstraintName("fk_upa_order");
        builder.HasOne(a => a.Program).WithMany().HasForeignKey(a => a.ProgramId).HasConstraintName("fk_upa_program");
        builder.HasOne(a => a.Duration).WithMany().HasForeignKey(a => a.DurationId).HasConstraintName("fk_upa_duration");
        builder.HasOne(a => a.Expert).WithMany().HasForeignKey(a => a.ExpertId).HasConstraintName("fk_upa_expert");
    }
}

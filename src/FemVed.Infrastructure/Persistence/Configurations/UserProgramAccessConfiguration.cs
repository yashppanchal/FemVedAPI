using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="UserProgramAccess"/> → <c>user_program_access</c>.</summary>
internal sealed class UserProgramAccessConfiguration : IEntityTypeConfiguration<UserProgramAccess>
{
    // ValueConverter lambdas compile to expression trees — switch expressions are not allowed.
    // Use static helper methods instead.
    private static readonly ValueConverter<UserProgramAccessStatus, string> StatusConverter =
        new(v => ToDb(v), v => FromDb(v));

    private static string ToDb(UserProgramAccessStatus v)
    {
        if (v == UserProgramAccessStatus.NotStarted) return "NOT_STARTED";
        if (v == UserProgramAccessStatus.Active)     return "ACTIVE";
        if (v == UserProgramAccessStatus.Paused)     return "PAUSED";
        if (v == UserProgramAccessStatus.Completed)  return "COMPLETED";
        if (v == UserProgramAccessStatus.Cancelled)  return "CANCELLED";
        return v.ToString().ToUpperInvariant();
    }

    private static UserProgramAccessStatus FromDb(string v)
    {
        if (v == "NOT_STARTED") return UserProgramAccessStatus.NotStarted;
        if (v == "ACTIVE")      return UserProgramAccessStatus.Active;
        if (v == "PAUSED")      return UserProgramAccessStatus.Paused;
        if (v == "COMPLETED")   return UserProgramAccessStatus.Completed;
        if (v == "CANCELLED")   return UserProgramAccessStatus.Cancelled;
        return Enum.Parse<UserProgramAccessStatus>(v, true);
    }

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
            .HasConversion(StatusConverter);
        builder.Property(a => a.ReminderSent).HasColumnName("reminder_sent").HasDefaultValue(false);
        builder.Property(a => a.ScheduledStartAt).HasColumnName("scheduled_start_at");
        builder.Property(a => a.StartReminderSentAt).HasColumnName("start_reminder_sent_at");
        builder.Property(a => a.RequestedStartDate).HasColumnName("requested_start_date");
        builder.Property(a => a.StartRequestStatus).HasColumnName("start_request_status").HasMaxLength(20)
            .HasConversion(v => v.HasValue ? v.Value.ToString() : null, v => v == null ? (Domain.Enums.StartRequestStatus?)null : Enum.Parse<Domain.Enums.StartRequestStatus>(v));
        builder.Property(a => a.EndDate).HasColumnName("end_date");
        builder.Property(a => a.StartedAt).HasColumnName("started_at");
        builder.Property(a => a.PausedAt).HasColumnName("paused_at");
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at");
        builder.Property(a => a.EndedBy).HasColumnName("ended_by");
        builder.Property(a => a.EndedByRole).HasColumnName("ended_by_role").HasMaxLength(20);
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

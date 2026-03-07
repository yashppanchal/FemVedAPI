using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramSessionLog"/> → <c>program_session_log</c>.</summary>
internal sealed class ProgramSessionLogConfiguration : IEntityTypeConfiguration<ProgramSessionLog>
{
    private static readonly ValueConverter<SessionAction, string> ActionConverter =
        new(
            v => v.ToString().ToUpperInvariant(),
            v => Enum.Parse<SessionAction>(v, true));

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramSessionLog> builder)
    {
        builder.ToTable("program_session_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(l => l.AccessId).HasColumnName("access_id").IsRequired();
        builder.Property(l => l.Action)
            .HasColumnName("action")
            .HasMaxLength(20)
            .HasConversion(ActionConverter);
        builder.Property(l => l.PerformedBy).HasColumnName("performed_by").IsRequired();
        builder.Property(l => l.PerformedByRole).HasColumnName("performed_by_role").HasMaxLength(20).IsRequired();
        builder.Property(l => l.Note).HasColumnName("note");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(l => l.AccessId).HasDatabaseName("idx_psl_access_id");

        builder.HasOne(l => l.Access)
            .WithMany(a => a.SessionLogs)
            .HasForeignKey(l => l.AccessId)
            .HasConstraintName("fk_psl_access");
    }
}

using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramDuration"/> → <c>program_durations</c>.</summary>
internal sealed class ProgramDurationConfiguration : IEntityTypeConfiguration<ProgramDuration>
{
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramDuration> builder)
    {
        builder.ToTable("program_durations");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(d => d.Label).HasColumnName("label").HasMaxLength(50).IsRequired();
        builder.Property(d => d.Weeks).HasColumnName("weeks").HasColumnType("smallint").IsRequired();
        builder.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(d => d.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(d => d.ProgramId).HasDatabaseName("idx_program_durations_program_id");

        builder.HasOne(d => d.Program)
            .WithMany(p => p.Durations)
            .HasForeignKey(d => d.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_durations_program");

        builder.HasData(
            new ProgramDuration { Id = Guid.Parse("66666666-0000-0000-0000-000000000001"), ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000001"), Label = "6 weeks", Weeks = 6, SortOrder = 1, IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new ProgramDuration { Id = Guid.Parse("66666666-0000-0000-0000-000000000002"), ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000002"), Label = "4 weeks", Weeks = 4, SortOrder = 1, IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new ProgramDuration { Id = Guid.Parse("66666666-0000-0000-0000-000000000003"), ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000003"), Label = "8 weeks", Weeks = 8, SortOrder = 1, IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new ProgramDuration { Id = Guid.Parse("66666666-0000-0000-0000-000000000004"), ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000004"), Label = "4 weeks", Weeks = 4, SortOrder = 1, IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded },
            new ProgramDuration { Id = Guid.Parse("66666666-0000-0000-0000-000000000005"), ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000005"), Label = "8 weeks", Weeks = 8, SortOrder = 1, IsActive = true, CreatedAt = Seeded, UpdatedAt = Seeded }
        );
    }
}

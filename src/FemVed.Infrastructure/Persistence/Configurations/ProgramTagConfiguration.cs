using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramTag"/> → <c>program_tags</c>.</summary>
internal sealed class ProgramTagConfiguration : IEntityTypeConfiguration<ProgramTag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramTag> builder)
    {
        builder.ToTable("program_tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(t => t.Tag).HasColumnName("tag").HasMaxLength(100).IsRequired();

        builder.HasIndex(t => new { t.ProgramId, t.Tag }).IsUnique().HasDatabaseName("uq_program_tags_program_tag");
        builder.HasIndex(t => t.ProgramId).HasDatabaseName("idx_program_tags_program_id");
        builder.HasIndex(t => t.Tag).HasDatabaseName("idx_program_tags_tag");

        builder.HasOne(t => t.Program)
            .WithMany(p => p.Tags)
            .HasForeignKey(t => t.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_tags_program");

        // Seed tags
        var p1 = Guid.Parse("55555555-0000-0000-0000-000000000001");
        var p2 = Guid.Parse("55555555-0000-0000-0000-000000000002");
        var p3 = Guid.Parse("55555555-0000-0000-0000-000000000003");
        var p4 = Guid.Parse("55555555-0000-0000-0000-000000000004");
        var p5 = Guid.Parse("55555555-0000-0000-0000-000000000005");

        builder.HasData(
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0001-0001-0000-000000000001"), ProgramId = p1, Tag = "stress" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0001-0002-0000-000000000001"), ProgramId = p1, Tag = "hormones" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0001-0003-0000-000000000001"), ProgramId = p1, Tag = "ayurveda" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0001-0004-0000-000000000001"), ProgramId = p1, Tag = "sleep" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0002-0001-0000-000000000001"), ProgramId = p2, Tag = "perimenopause" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0002-0002-0000-000000000001"), ProgramId = p2, Tag = "hormones" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0002-0003-0000-000000000001"), ProgramId = p2, Tag = "ayurveda" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0002-0004-0000-000000000001"), ProgramId = p2, Tag = "ageing" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0003-0001-0000-000000000001"), ProgramId = p3, Tag = "pcos" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0003-0002-0000-000000000001"), ProgramId = p3, Tag = "hormones" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0003-0003-0000-000000000001"), ProgramId = p3, Tag = "fertility" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0003-0004-0000-000000000001"), ProgramId = p3, Tag = "metabolism" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0004-0001-0000-000000000001"), ProgramId = p4, Tag = "menopause" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0004-0002-0000-000000000001"), ProgramId = p4, Tag = "metabolism" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0004-0003-0000-000000000001"), ProgramId = p4, Tag = "weight" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0004-0004-0000-000000000001"), ProgramId = p4, Tag = "hormones" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0005-0001-0000-000000000001"), ProgramId = p5, Tag = "hormones" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0005-0002-0000-000000000001"), ProgramId = p5, Tag = "gut-health" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0005-0003-0000-000000000001"), ProgramId = p5, Tag = "pms" },
            new ProgramTag { Id = Guid.Parse("eeeeeeee-0005-0004-0000-000000000001"), ProgramId = p5, Tag = "energy" }
        );
    }
}

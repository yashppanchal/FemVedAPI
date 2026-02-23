using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramWhoIsThisFor"/> → <c>program_who_is_this_for</c>.</summary>
internal sealed class ProgramWhoIsThisForConfiguration : IEntityTypeConfiguration<ProgramWhoIsThisFor>
{
    private static readonly Guid Prog1 = Guid.Parse("55555555-0000-0000-0000-000000000001");
    private static readonly Guid Prog3 = Guid.Parse("55555555-0000-0000-0000-000000000003");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramWhoIsThisFor> builder)
    {
        builder.ToTable("program_who_is_this_for");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.ItemText).HasColumnName("item_text").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.ProgramId).HasDatabaseName("idx_program_who_is_this_for_program_id");

        builder.HasOne(x => x.Program)
            .WithMany(p => p.WhoIsThisFor)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_who_is_this_for_program");

        builder.HasData(
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0001-0000-0000-000000000001"), ProgramId = Prog1, ItemText = "Women experiencing hormonal imbalance, irregular cycles, PMS, or fatigue", SortOrder = 1 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0001-0000-0000-000000000002"), ProgramId = Prog1, ItemText = "Individuals dealing with chronic stress, burnout, or sleep disturbances", SortOrder = 2 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0001-0000-0000-000000000003"), ProgramId = Prog1, ItemText = "Women navigating thyroid, metabolic, or adrenal health concerns", SortOrder = 3 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0001-0000-0000-000000000004"), ProgramId = Prog1, ItemText = "Anyone wanting structured, sustainable lifestyle tools rooted in Ayurveda", SortOrder = 4 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0003-0000-0000-000000000001"), ProgramId = Prog3, ItemText = "Women aged 25–40 diagnosed with PCOS or strongly suspecting PCOS", SortOrder = 1 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0003-0000-0000-000000000002"), ProgramId = Prog3, ItemText = "Women experiencing irregular or absent cycles, fertility challenges, or hormonal acne", SortOrder = 2 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0003-0000-0000-000000000003"), ProgramId = Prog3, ItemText = "Individuals struggling with stubborn weight gain, sugar cravings, or insulin resistance", SortOrder = 3 },
            new ProgramWhoIsThisFor { Id = Guid.Parse("dddddddd-0003-0000-0000-000000000004"), ProgramId = Prog3, ItemText = "Women preparing for conception and seeking to optimise reproductive health", SortOrder = 4 }
        );
    }
}

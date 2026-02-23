using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramWhatYouGet"/> → <c>program_what_you_get</c>.</summary>
internal sealed class ProgramWhatYouGetConfiguration : IEntityTypeConfiguration<ProgramWhatYouGet>
{
    private static readonly Guid Prog1 = Guid.Parse("55555555-0000-0000-0000-000000000001");
    private static readonly Guid Prog3 = Guid.Parse("55555555-0000-0000-0000-000000000003");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramWhatYouGet> builder)
    {
        builder.ToTable("program_what_you_get");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.ItemText).HasColumnName("item_text").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.ProgramId).HasDatabaseName("idx_program_what_you_get_program_id");

        builder.HasOne(x => x.Program)
            .WithMany(p => p.WhatYouGet)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_what_you_get_program");

        builder.HasData(
            // Program 1
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000001"), ProgramId = Prog1, ItemText = "Personalised Dosha, lifestyle, and hormonal pattern assessment", SortOrder = 1 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000002"), ProgramId = Prog1, ItemText = "Step-by-step weekly structure to regulate stress and support hormone balance", SortOrder = 2 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000003"), ProgramId = Prog1, ItemText = "Ayurvedic self-care rituals including daily rhythm and nervous system calming practices", SortOrder = 3 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000004"), ProgramId = Prog1, ItemText = "Practical breathwork and relaxation techniques to stabilise stress hormones", SortOrder = 4 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000005"), ProgramId = Prog1, ItemText = "Hormone-supportive dietary and digestion strengthening guidelines", SortOrder = 5 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000006"), ProgramId = Prog1, ItemText = "Long-term maintenance plan to sustain hormonal stability", SortOrder = 6 },
            // Program 3
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000001"), ProgramId = Prog3, ItemText = "Personalised herbal tincture formulas designed to address your PCOS root cause", SortOrder = 1 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000002"), ProgramId = Prog3, ItemText = "Custom supplement protocol to support metabolic and hormonal balance", SortOrder = 2 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000003"), ProgramId = Prog3, ItemText = "Tailored 5-day metabolic cleanse to reset blood sugar and inflammation pathways", SortOrder = 3 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000004"), ProgramId = Prog3, ItemText = "28-day hormone-supportive metabolic meal plan", SortOrder = 4 },
            new ProgramWhatYouGet { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000005"), ProgramId = Prog3, ItemText = "Identification of your PCOS subtype: insulin-driven, inflammatory, or androgen-dominant", SortOrder = 5 }
        );
    }
}

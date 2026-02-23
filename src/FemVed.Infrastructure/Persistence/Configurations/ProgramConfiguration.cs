using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="Program"/> → <c>programs</c>.</summary>
internal sealed class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    private static readonly Guid HormonalCatId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid ExpertPrathima = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid ExpertKimberly = Guid.Parse("44444444-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Program> builder)
    {
        builder.ToTable("programs");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(p => p.ExpertId).HasColumnName("expert_id").IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(300).IsRequired();
        builder.Property(p => p.GridDescription).HasColumnName("grid_description").HasMaxLength(500).IsRequired();
        builder.Property(p => p.GridImageUrl).HasColumnName("grid_image_url");
        builder.Property(p => p.Overview).HasColumnName("overview").IsRequired();
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(
                v => v.ToString().ToUpperInvariant().Replace("PENDINGREVIEW", "PENDING_REVIEW"),
                v => v == "PENDING_REVIEW" ? ProgramStatus.PendingReview : Enum.Parse<ProgramStatus>(v, true));
        builder.Property(p => p.StartDate).HasColumnName("start_date");
        builder.Property(p => p.EndDate).HasColumnName("end_date");
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("uq_programs_slug");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("idx_programs_category_id");
        builder.HasIndex(p => p.ExpertId).HasDatabaseName("idx_programs_expert_id");
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_programs_status");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Programs)
            .HasForeignKey(p => p.CategoryId)
            .HasConstraintName("fk_programs_category");

        builder.HasOne(p => p.Expert)
            .WithMany(e => e.Programs)
            .HasForeignKey(p => p.ExpertId)
            .HasConstraintName("fk_programs_expert");

        builder.HasData(
            new Program { Id = Guid.Parse("55555555-0000-0000-0000-000000000001"), CategoryId = HormonalCatId, ExpertId = ExpertPrathima, Name = "Break the Stress–Hormone–Health Triangle", Slug = "break-stress-hormone-health-triangle", GridDescription = "A 6-week Ayurvedic program to reset stress patterns, restore hormonal balance, and rebuild daily rhythm.", Overview = "Did you know that chronic stress can quietly disrupt hormonal balance, digestion, sleep, and long-term vitality? In this 6-week guided program, you will move through structured, personalised phases designed to regulate the stress response, improve digestion and hormone metabolism, nourish reproductive and adrenal health, and restore circadian rhythm. Each phase introduces practical Ayurvedic lifestyle tools, self-care rituals, dietary guidance, and stress-regulation techniques that fit into everyday life.", Status = ProgramStatus.Published, SortOrder = 1, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded },
            new Program { Id = Guid.Parse("55555555-0000-0000-0000-000000000002"), CategoryId = HormonalCatId, ExpertId = ExpertPrathima, Name = "Balancing & Managing Perimenopause with Ayurveda", Slug = "balancing-perimenopause-ayurveda", GridDescription = "A 4-week Ayurvedic program to stabilise hormonal transitions, support emotional balance, and strengthen long-term vitality.", Overview = "In Ayurveda, perimenopause reflects natural changes in reproductive tissues and dosha balance, often marked by fluctuations in Vata and Pitta. In this 4-week guided program, you will move through personalised Ayurvedic phases designed to understand perimenopausal changes, regulate hormonal fluctuations, support digestion and tissue nourishment, and establish lifestyle rhythms that ease this transition.", Status = ProgramStatus.Published, SortOrder = 2, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded },
            new Program { Id = Guid.Parse("55555555-0000-0000-0000-000000000003"), CategoryId = HormonalCatId, ExpertId = ExpertKimberly, Name = "The Metabolic PCOS Reset", Slug = "metabolic-pcos-reset", GridDescription = "An 8-week naturopath-led program to restore metabolic balance, regulate hormones, and support fertility.", Overview = "PCOS is not just a reproductive condition but a metabolic and hormonal imbalance often driven by insulin resistance, androgen excess, inflammation, and chronic stress. In this 8-week program, you will work through a structured, personalised approach designed to identify the metabolic drivers behind your PCOS and reverse them using herbal medicine, nutrition therapy, and hormone-supportive lifestyle strategies.", Status = ProgramStatus.Published, SortOrder = 3, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded },
            new Program { Id = Guid.Parse("55555555-0000-0000-0000-000000000004"), CategoryId = HormonalCatId, ExpertId = ExpertKimberly, Name = "28-Day Mastering Midlife Metabolism Method", Slug = "28-day-midlife-metabolism-method", GridDescription = "A 28-day food-based protocol to balance hormones, reset metabolism, and restore energy stability during midlife.", Overview = "Weight gain, fatigue, and metabolic slowdown during midlife are rarely just about calories or exercise. As women transition through perimenopause and menopause, natural shifts in oestrogen, progesterone, insulin, and cortisol significantly impact metabolism. This method integrates cycle-synced nutrition, strategic fasting windows, seed cycling, and lifestyle rhythm correction to support hormonal balance without extreme dieting.", Status = ProgramStatus.Published, SortOrder = 4, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded },
            new Program { Id = Guid.Parse("55555555-0000-0000-0000-000000000005"), CategoryId = HormonalCatId, ExpertId = ExpertKimberly, Name = "The Happy Hormone Method", Slug = "happy-hormone-method", GridDescription = "An 8-week root-cause naturopath program to rebalance hormones, restore energy, and reclaim your natural flow.", Overview = "Hormonal symptoms like PMS, bloating, mood swings, fatigue, and sleep disruption are often signals of deeper imbalances involving inflammation, gut health, stress hormones, and nutrient deficiencies. In this 8-week 1:1 program, you will follow a structured, personalised treatment pathway to identify root causes and support healing through targeted herbal medicine, nutrition therapy, and lifestyle rhythm correction.", Status = ProgramStatus.Published, SortOrder = 5, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded }
        );
    }
}

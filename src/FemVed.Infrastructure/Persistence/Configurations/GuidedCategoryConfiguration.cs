using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="GuidedCategory"/> → <c>guided_categories</c>.</summary>
internal sealed class GuidedCategoryConfiguration : IEntityTypeConfiguration<GuidedCategory>
{
    private static readonly Guid DomainId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GuidedCategory> builder)
    {
        builder.ToTable("guided_categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(c => c.CategoryType).HasColumnName("category_type").HasMaxLength(100).IsRequired();
        builder.Property(c => c.HeroTitle).HasColumnName("hero_title").HasMaxLength(300).IsRequired();
        builder.Property(c => c.HeroSubtext).HasColumnName("hero_subtext").IsRequired();
        builder.Property(c => c.CtaLabel).HasColumnName("cta_label").HasMaxLength(100);
        builder.Property(c => c.CtaLink).HasColumnName("cta_link").HasMaxLength(300);
        builder.Property(c => c.PageHeader).HasColumnName("page_header").HasMaxLength(300);
        builder.Property(c => c.ImageUrl).HasColumnName("image_url");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("uq_guided_categories_slug");
        builder.HasIndex(c => c.DomainId).HasDatabaseName("idx_guided_categories_domain_id");
        builder.HasIndex(c => c.ParentId).HasDatabaseName("idx_guided_categories_parent_id");

        builder.HasOne(c => c.Domain)
            .WithMany(d => d.Categories)
            .HasForeignKey(c => c.DomainId)
            .HasConstraintName("fk_guided_categories_domain");

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .HasConstraintName("fk_guided_categories_parent");

        builder.HasData(
            new GuidedCategory
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), DomainId = DomainId, ParentId = null,
                Name = "Hormonal Health Support", Slug = "hormonal-health-support",
                CategoryType = "Hormonal Health Support", HeroTitle = "Get Guided Hormonal Care",
                HeroSubtext = "When hormonal changes feel overwhelming and online advice leaves you confused, you deserve guidance you can trust. Get one-to-one support from experienced practitioners and create a personalised wellness plan that fits your life, accessible from anywhere.",
                CtaLabel = "Book Your Program", CtaLink = "/guided/hormonal-health-support",
                PageHeader = "Choose and book the guided journey that best fits your needs, goals, and life right now.",
                SortOrder = 1, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            },
            new GuidedCategory
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), DomainId = DomainId, ParentId = null,
                Name = "Mental and Spiritual Wellbeing", Slug = "mental-spiritual-wellbeing",
                CategoryType = "Mind and Spirituality", HeroTitle = "Begin Your Personal Mind Support",
                HeroSubtext = "When constant advice and quick fixes leave you feeling overwhelmed, the right guidance helps you slow down. Get one-to-one support from experienced counsellors and spiritual practitioners to find emotional clarity and inner balance, from the comfort of your home.",
                CtaLabel = "Book Your Program", CtaLink = "/guided/mental-spiritual-wellbeing",
                PageHeader = "Choose and book the guided journey that best fits your needs, goals, and life right now.",
                SortOrder = 2, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            },
            new GuidedCategory
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), DomainId = DomainId, ParentId = null,
                Name = "Longevity and Healthy Ageing", Slug = "longevity-healthy-ageing",
                CategoryType = "Longevity", HeroTitle = "Plan Your Long-Term Health",
                HeroSubtext = "When longevity trends and conflicting wellness advice leave you confused, the right guidance brings clarity. Work one-to-one with experienced experts to create a personalised longevity plan rooted in science, lifestyle, and prevention, accessible from home.",
                CtaLabel = "Book Your Program", CtaLink = "/guided/longevity-healthy-ageing",
                PageHeader = "Choose and book the guided journey that best fits your needs, goals, and life right now.",
                SortOrder = 3, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            },
            new GuidedCategory
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), DomainId = DomainId, ParentId = null,
                Name = "Fitness and Personal Care Support", Slug = "fitness-personal-care",
                CategoryType = "Fitness and Body Care", HeroTitle = "Book Your Personal Wellness Program",
                HeroSubtext = "When online fitness advice leaves you unsure what your body truly needs, personalised guidance makes the difference. Get one-to-one support to build a fitness plan that respects your strength, recovery, and rhythm, from the comfort of your home.",
                CtaLabel = "Book Your Program", CtaLink = "/guided/fitness-personal-care",
                PageHeader = "Choose and book the guided journey that best fits your needs, goals, and life right now.",
                SortOrder = 4, IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            }
        );
    }
}

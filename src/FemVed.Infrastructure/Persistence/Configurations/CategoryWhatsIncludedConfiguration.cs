using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="CategoryWhatsIncluded"/> → <c>category_whats_included</c>.</summary>
internal sealed class CategoryWhatsIncludedConfiguration : IEntityTypeConfiguration<CategoryWhatsIncluded>
{
    private static readonly Guid HormonalCatId = Guid.Parse("22222222-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategoryWhatsIncluded> builder)
    {
        builder.ToTable("category_whats_included");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.ItemText).HasColumnName("item_text").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.CategoryId).HasDatabaseName("idx_category_whats_included_category_id");

        builder.HasOne(x => x.Category)
            .WithMany(c => c.WhatsIncluded)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_category_whats_included_category");

        builder.HasData(
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000001"), CategoryId = HormonalCatId, ItemText = "In-depth health assessment with a hormonal wellness expert", SortOrder = 1 },
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000002"), CategoryId = HormonalCatId, ItemText = "One-to-one guidance and ongoing support", SortOrder = 2 },
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000003"), CategoryId = HormonalCatId, ItemText = "Personalised 4–12 week wellness plan", SortOrder = 3 },
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000004"), CategoryId = HormonalCatId, ItemText = "Care tailored to your hormones, life stage, and goals", SortOrder = 4 },
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000005"), CategoryId = HormonalCatId, ItemText = "Customised diet and lifestyle plan with shopping guidance", SortOrder = 5 },
            new CategoryWhatsIncluded { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000006"), CategoryId = HormonalCatId, ItemText = "Support for concerns like PCOS, endometriosis, fertility, and cycle health", SortOrder = 6 }
        );
    }
}

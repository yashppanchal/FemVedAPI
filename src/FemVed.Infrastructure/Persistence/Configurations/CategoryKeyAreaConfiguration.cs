using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="CategoryKeyArea"/> → <c>category_key_areas</c>.</summary>
internal sealed class CategoryKeyAreaConfiguration : IEntityTypeConfiguration<CategoryKeyArea>
{
    private static readonly Guid HormonalCatId = Guid.Parse("22222222-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategoryKeyArea> builder)
    {
        builder.ToTable("category_key_areas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.AreaText).HasColumnName("area_text").HasMaxLength(300).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.CategoryId).HasDatabaseName("idx_category_key_areas_category_id");

        builder.HasOne(x => x.Category)
            .WithMany(c => c.KeyAreas)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_category_key_areas_category");

        builder.HasData(
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000001"), CategoryId = HormonalCatId, AreaText = "Preconception and fertility nutrition", SortOrder = 1 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000002"), CategoryId = HormonalCatId, AreaText = "Pregnancy and postpartum support", SortOrder = 2 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000003"), CategoryId = HormonalCatId, AreaText = "PCOS, endometriosis, and cycle health", SortOrder = 3 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000004"), CategoryId = HormonalCatId, AreaText = "Hormone and stress balance", SortOrder = 4 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000005"), CategoryId = HormonalCatId, AreaText = "Intuitive eating and metabolic health", SortOrder = 5 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000006"), CategoryId = HormonalCatId, AreaText = "Menopause and perimenopause care", SortOrder = 6 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000007"), CategoryId = HormonalCatId, AreaText = "Diabetes and weight management", SortOrder = 7 },
            new CategoryKeyArea { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000008"), CategoryId = HormonalCatId, AreaText = "Life stage hormonal guidance", SortOrder = 8 }
        );
    }
}

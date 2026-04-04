using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryDomain"/> → <c>library_domain</c>.</summary>
internal sealed class LibraryDomainConfiguration : IEntityTypeConfiguration<LibraryDomain>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryDomain> builder)
    {
        builder.ToTable("library_domain");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(d => d.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasColumnName("description");
        builder.Property(d => d.HeroImageDesktop).HasColumnName("hero_image_desktop");
        builder.Property(d => d.HeroImageMobile).HasColumnName("hero_image_mobile");
        builder.Property(d => d.HeroImagePortrait).HasColumnName("hero_image_portrait");
        builder.Property(d => d.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(d => d.Slug).IsUnique().HasDatabaseName("uq_library_domain_slug");

        // Seed: "Wellness Library"
        builder.HasData(new LibraryDomain
        {
            Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
            Name = "Wellness Library",
            Slug = "wellness-library",
            Description = "Recorded wellness video content — masterclasses and series for self-paced learning.",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

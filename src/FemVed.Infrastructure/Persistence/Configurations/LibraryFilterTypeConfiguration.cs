using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryFilterType"/> → <c>library_filter_types</c>.</summary>
internal sealed class LibraryFilterTypeConfiguration : IEntityTypeConfiguration<LibraryFilterType>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryFilterType> builder)
    {
        builder.ToTable("library_filter_types");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(f => f.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(f => f.FilterKey).HasColumnName("filter_key").HasMaxLength(100).IsRequired();
        builder.Property(f => f.FilterTarget)
            .HasColumnName("filter_target")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<FilterTarget>(v, true));
        builder.Property(f => f.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(f => f.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(f => f.DomainId).HasDatabaseName("idx_library_filter_types_domain_id");

        builder.HasOne(f => f.Domain)
            .WithMany(d => d.FilterTypes)
            .HasForeignKey(f => f.DomainId)
            .HasConstraintName("fk_library_filter_types_domain");

        // Seed sample filter types
        var domainId = Guid.Parse("22222222-0000-0000-0000-000000000001");
        builder.HasData(
            new LibraryFilterType
            {
                Id = Guid.Parse("33333333-0000-0000-0000-000000000001"),
                DomainId = domainId,
                Name = "Masterclasses",
                FilterKey = "masterclass",
                FilterTarget = FilterTarget.VideoType,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new LibraryFilterType
            {
                Id = Guid.Parse("33333333-0000-0000-0000-000000000002"),
                DomainId = domainId,
                Name = "Series",
                FilterKey = "series",
                FilterTarget = FilterTarget.VideoType,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new LibraryFilterType
            {
                Id = Guid.Parse("33333333-0000-0000-0000-000000000003"),
                DomainId = domainId,
                Name = "Mindfulness",
                FilterKey = "mindfulness",
                FilterTarget = FilterTarget.Category,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        );
    }
}

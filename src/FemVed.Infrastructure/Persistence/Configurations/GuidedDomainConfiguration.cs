using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="GuidedDomain"/> → <c>guided_domains</c>.</summary>
internal sealed class GuidedDomainConfiguration : IEntityTypeConfiguration<GuidedDomain>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GuidedDomain> builder)
    {
        builder.ToTable("guided_domains");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(d => d.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(d => d.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(d => d.Slug).IsUnique().HasDatabaseName("uq_guided_domains_slug");

        // Seed: "Guided 1:1 Care"
        builder.HasData(new GuidedDomain
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Name = "Guided 1:1 Care",
            Slug = "guided-1-1-care",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}

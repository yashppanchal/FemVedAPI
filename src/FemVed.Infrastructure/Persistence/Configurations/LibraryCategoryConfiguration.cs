using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryCategory"/> → <c>library_categories</c>.</summary>
internal sealed class LibraryCategoryConfiguration : IEntityTypeConfiguration<LibraryCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryCategory> builder)
    {
        builder.ToTable("library_categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(300).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.CardImage).HasColumnName("card_image");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("uq_library_categories_slug");
        builder.HasIndex(c => c.DomainId).HasDatabaseName("idx_library_categories_domain_id");

        builder.HasOne(c => c.Domain)
            .WithMany(d => d.Categories)
            .HasForeignKey(c => c.DomainId)
            .HasConstraintName("fk_library_categories_domain");
    }
}

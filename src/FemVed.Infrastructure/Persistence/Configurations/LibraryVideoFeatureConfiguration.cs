using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideoFeature"/> → <c>library_video_features</c>.</summary>
internal sealed class LibraryVideoFeatureConfiguration : IEntityTypeConfiguration<LibraryVideoFeature>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideoFeature> builder)
    {
        builder.ToTable("library_video_features");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(f => f.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(f => f.Icon).HasColumnName("icon").HasMaxLength(10).IsRequired();
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(f => f.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(f => f.VideoId).HasDatabaseName("idx_library_video_features_video_id");

        builder.HasOne(f => f.Video)
            .WithMany(v => v.Features)
            .HasForeignKey(f => f.VideoId)
            .HasConstraintName("fk_library_video_features_video");
    }
}

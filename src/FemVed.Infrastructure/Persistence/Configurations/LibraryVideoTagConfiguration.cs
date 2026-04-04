using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideoTag"/> → <c>library_video_tags</c>.</summary>
internal sealed class LibraryVideoTagConfiguration : IEntityTypeConfiguration<LibraryVideoTag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideoTag> builder)
    {
        builder.ToTable("library_video_tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(t => t.Tag).HasColumnName("tag").HasMaxLength(100).IsRequired();
        builder.Property(t => t.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(t => t.VideoId).HasDatabaseName("idx_library_video_tags_video_id");

        builder.HasOne(t => t.Video)
            .WithMany(v => v.Tags)
            .HasForeignKey(t => t.VideoId)
            .HasConstraintName("fk_library_video_tags_video");
    }
}

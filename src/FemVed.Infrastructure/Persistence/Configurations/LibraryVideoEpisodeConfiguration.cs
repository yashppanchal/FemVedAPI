using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideoEpisode"/> → <c>library_video_episodes</c>.</summary>
internal sealed class LibraryVideoEpisodeConfiguration : IEntityTypeConfiguration<LibraryVideoEpisode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideoEpisode> builder)
    {
        builder.ToTable("library_video_episodes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(e => e.EpisodeNumber).HasColumnName("episode_number").IsRequired();
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(50);
        builder.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
        builder.Property(e => e.StreamUrl).HasColumnName("stream_url");
        builder.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
        builder.Property(e => e.IsFreePreview).HasColumnName("is_free_preview").HasDefaultValue(false);
        builder.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.VideoId, e.EpisodeNumber }).IsUnique().HasDatabaseName("uq_library_video_episodes_video_number");
        builder.HasIndex(e => e.VideoId).HasDatabaseName("idx_library_video_episodes_video_id");

        builder.HasOne(e => e.Video)
            .WithMany(v => v.Episodes)
            .HasForeignKey(e => e.VideoId)
            .HasConstraintName("fk_library_video_episodes_video");
    }
}

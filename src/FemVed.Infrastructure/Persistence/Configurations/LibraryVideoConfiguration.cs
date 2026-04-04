using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideo"/> → <c>library_videos</c>.</summary>
internal sealed class LibraryVideoConfiguration : IEntityTypeConfiguration<LibraryVideo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideo> builder)
    {
        builder.ToTable("library_videos");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(v => v.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(v => v.ExpertId).HasColumnName("expert_id").IsRequired();
        builder.Property(v => v.PriceTierId).HasColumnName("price_tier_id").IsRequired();
        builder.Property(v => v.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(v => v.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        builder.Property(v => v.Synopsis).HasColumnName("synopsis");
        builder.Property(v => v.Description).HasColumnName("description");
        builder.Property(v => v.CardImage).HasColumnName("card_image");
        builder.Property(v => v.HeroImage).HasColumnName("hero_image");
        builder.Property(v => v.IconEmoji).HasColumnName("icon_emoji").HasMaxLength(10);
        builder.Property(v => v.GradientClass).HasColumnName("gradient_class").HasMaxLength(50);
        builder.Property(v => v.TrailerUrl).HasColumnName("trailer_url");
        builder.Property(v => v.StreamUrl).HasColumnName("stream_url");
        builder.Property(v => v.VideoType)
            .HasColumnName("video_type")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<VideoType>(v, true));
        builder.Property(v => v.TotalDuration).HasColumnName("total_duration").HasMaxLength(50);
        builder.Property(v => v.TotalDurationSeconds).HasColumnName("total_duration_seconds");
        builder.Property(v => v.ReleaseYear).HasColumnName("release_year").HasMaxLength(4);
        builder.Property(v => v.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        builder.Property(v => v.FeaturedLabel).HasColumnName("featured_label").HasMaxLength(200);
        builder.Property(v => v.FeaturedPosition).HasColumnName("featured_position");
        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<VideoStatus>(v, true));
        builder.Property(v => v.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(v => v.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(v => v.Slug).IsUnique().HasDatabaseName("uq_library_videos_slug");
        builder.HasIndex(v => v.CategoryId).HasDatabaseName("idx_library_videos_category_id");
        builder.HasIndex(v => v.ExpertId).HasDatabaseName("idx_library_videos_expert_id");
        builder.HasIndex(v => v.Status).HasDatabaseName("idx_library_videos_status");
        builder.HasIndex(v => v.IsFeatured).HasDatabaseName("idx_library_videos_is_featured");

        builder.HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryId)
            .HasConstraintName("fk_library_videos_category");

        builder.HasOne(v => v.Expert)
            .WithMany()
            .HasForeignKey(v => v.ExpertId)
            .HasConstraintName("fk_library_videos_expert");

        builder.HasOne(v => v.PriceTier)
            .WithMany(t => t.Videos)
            .HasForeignKey(v => v.PriceTierId)
            .HasConstraintName("fk_library_videos_price_tier");
    }
}

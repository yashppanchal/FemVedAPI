using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="UserVideoReview"/> → <c>user_video_reviews</c>.</summary>
internal sealed class UserVideoReviewConfiguration : IEntityTypeConfiguration<UserVideoReview>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserVideoReview> builder)
    {
        builder.ToTable("user_video_reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(r => r.Rating).HasColumnName("rating").IsRequired();
        builder.Property(r => r.ReviewText).HasColumnName("review_text");
        builder.Property(r => r.IsApproved).HasColumnName("is_approved").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(r => new { r.UserId, r.VideoId }).IsUnique().HasDatabaseName("uq_user_video_reviews_user_video");
        builder.HasIndex(r => r.VideoId).HasDatabaseName("idx_user_video_reviews_video_id");

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("fk_user_video_reviews_user");

        builder.HasOne(r => r.Video)
            .WithMany()
            .HasForeignKey(r => r.VideoId)
            .HasConstraintName("fk_user_video_reviews_video");

        // Rating must be between 1 and 5
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_user_video_reviews_rating",
            "rating >= 1 AND rating <= 5"));
    }
}

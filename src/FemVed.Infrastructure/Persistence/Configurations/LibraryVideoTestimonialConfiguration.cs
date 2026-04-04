using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="LibraryVideoTestimonial"/> → <c>library_video_testimonials</c>.</summary>
internal sealed class LibraryVideoTestimonialConfiguration : IEntityTypeConfiguration<LibraryVideoTestimonial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LibraryVideoTestimonial> builder)
    {
        builder.ToTable("library_video_testimonials");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(t => t.ReviewerName).HasColumnName("reviewer_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.ReviewText).HasColumnName("review_text").IsRequired();
        builder.Property(t => t.Rating).HasColumnName("rating").IsRequired();
        builder.Property(t => t.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(t => t.VideoId).HasDatabaseName("idx_library_video_testimonials_video_id");

        builder.HasOne(t => t.Video)
            .WithMany(v => v.Testimonials)
            .HasForeignKey(t => t.VideoId)
            .HasConstraintName("fk_library_video_testimonials_video");

        // Rating must be between 1 and 5
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_library_video_testimonials_rating",
            "rating >= 1 AND rating <= 5"));
    }
}

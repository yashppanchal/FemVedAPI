using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="UserLibraryAccess"/> → <c>user_library_access</c>.</summary>
internal sealed class UserLibraryAccessConfiguration : IEntityTypeConfiguration<UserLibraryAccess>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserLibraryAccess> builder)
    {
        builder.ToTable("user_library_access");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.VideoId).HasColumnName("video_id").IsRequired();
        builder.Property(a => a.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(a => a.PurchasedAt).HasColumnName("purchased_at").HasDefaultValueSql("NOW()");
        builder.Property(a => a.ExpiresAt).HasColumnName("expires_at");
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.LastWatchedAt).HasColumnName("last_watched_at");
        builder.Property(a => a.WatchProgressSeconds).HasColumnName("watch_progress_seconds").HasDefaultValue(0);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => new { a.UserId, a.VideoId }).IsUnique().HasDatabaseName("uq_user_library_access_user_video");
        builder.HasIndex(a => a.UserId).HasDatabaseName("idx_user_library_access_user_id");
        builder.HasIndex(a => a.VideoId).HasDatabaseName("idx_user_library_access_video_id");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("fk_user_library_access_user");

        builder.HasOne(a => a.Video)
            .WithMany(v => v.UserAccesses)
            .HasForeignKey(a => a.VideoId)
            .HasConstraintName("fk_user_library_access_video");

        builder.HasOne(a => a.Order)
            .WithMany(o => o.LibraryAccesses)
            .HasForeignKey(a => a.OrderId)
            .HasConstraintName("fk_user_library_access_order");
    }
}

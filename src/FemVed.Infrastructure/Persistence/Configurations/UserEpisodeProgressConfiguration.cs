using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="UserEpisodeProgress"/> → <c>user_episode_progress</c>.</summary>
internal sealed class UserEpisodeProgressConfiguration : IEntityTypeConfiguration<UserEpisodeProgress>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserEpisodeProgress> builder)
    {
        builder.ToTable("user_episode_progress");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.EpisodeId).HasColumnName("episode_id").IsRequired();
        builder.Property(p => p.WatchProgressSeconds).HasColumnName("watch_progress_seconds").HasDefaultValue(0);
        builder.Property(p => p.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
        builder.Property(p => p.LastWatchedAt).HasColumnName("last_watched_at");

        builder.HasIndex(p => new { p.UserId, p.EpisodeId }).IsUnique().HasDatabaseName("uq_user_episode_progress_user_episode");
        builder.HasIndex(p => p.UserId).HasDatabaseName("idx_user_episode_progress_user_id");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .HasConstraintName("fk_user_episode_progress_user");

        builder.HasOne(p => p.Episode)
            .WithMany(e => e.UserProgress)
            .HasForeignKey(p => p.EpisodeId)
            .HasConstraintName("fk_user_episode_progress_episode");
    }
}

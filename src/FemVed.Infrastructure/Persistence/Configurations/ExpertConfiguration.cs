using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="Expert"/> → <c>experts</c>.</summary>
internal sealed class ExpertConfiguration : IEntityTypeConfiguration<Expert>
{
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Expert> builder)
    {
        builder.ToTable("experts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Bio).HasColumnName("bio").IsRequired();
        builder.Property(e => e.GridDescription).HasColumnName("expert_grid_description").HasMaxLength(500);
        builder.Property(e => e.DetailedDescription).HasColumnName("expert_detailed_description");
        builder.Property(e => e.ProfileImageUrl).HasColumnName("profile_image_url");
        builder.Property(e => e.Specialisations).HasColumnName("specialisations").HasColumnType("text[]");
        builder.Property(e => e.YearsExperience).HasColumnName("years_experience").HasColumnType("smallint");
        builder.Property(e => e.Credentials).HasColumnName("credentials").HasColumnType("text[]");
        builder.Property(e => e.LocationCountry).HasColumnName("location_country").HasMaxLength(100);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("uq_experts_user_id");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_experts_user_id");

        builder.HasOne(e => e.User)
            .WithOne(u => u.ExpertProfile)
            .HasForeignKey<Expert>(e => e.UserId)
            .HasConstraintName("fk_experts_user");

        builder.HasData(
            new Expert
            {
                Id = Guid.Parse("44444444-0000-0000-0000-000000000001"),
                UserId = Guid.Parse("33333333-0000-0000-0000-000000000001"),
                DisplayName = "Dr. Prathima Nagesh",
                Title = "Ayurvedic Physician & Women's Health Specialist",
                Bio = "Dr. Prathima is a distinguished BAMS, MD Ayurvedic physician with over 25 years of clinical experience, specialising in women's health and holistic well-being. A trained Clinical Researcher (GCSRT) from Harvard Medical School, she blends classical Ayurvedic wisdom with evidence-informed clinical practice. Over the years, she has successfully supported women through complex health challenges including menstrual disorders, hormonal imbalances, fertility concerns, and chronic lifestyle-related conditions.",
                GridDescription = "BAMS, MD Ayurvedic physician with 25+ years of experience in women's hormonal health and Ayurvedic medicine.",
                DetailedDescription = null,
                Specialisations = new[] { "Hormonal Health", "Ayurveda", "Women's Wellness", "Perimenopause", "PCOS", "Fertility" },
                YearsExperience = 25,
                Credentials = new[] { "BAMS", "MD Ayurveda", "GCSRT - Harvard Medical School" },
                LocationCountry = "India",
                IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            },
            new Expert
            {
                Id = Guid.Parse("44444444-0000-0000-0000-000000000002"),
                UserId = Guid.Parse("33333333-0000-0000-0000-000000000002"),
                DisplayName = "Kimberly Parsons",
                Title = "Naturopath, Herbalist & Author",
                Bio = "Kimberly Parsons is the founder of Naturalli.me, an all-natural clinic focused on women's hormonal health. Australian-born and trained, she holds a Bachelor of Health Science in Naturopathy and brings over 20 years of experience supporting women through herbal medicine, nutrition, and lifestyle care. She is the internationally best-selling author of The Yoga Kitchen series and creator of the Naturalli 28Days app. Kimberly has led wellness retreats around the world, sharing her philosophy of healing through food, herbs, and rhythm-based living.",
                GridDescription = "Naturopath and herbalist with 20+ years experience. Best-selling author of The Yoga Kitchen series. Founder of Naturalli.me.",
                DetailedDescription = null,
                Specialisations = new[] { "Naturopathy", "Herbal Medicine", "PCOS", "Hormonal Health", "Metabolism", "Menopause" },
                YearsExperience = 20,
                Credentials = new[] { "Bachelor of Health Science in Naturopathy" },
                LocationCountry = "United Kingdom",
                IsActive = true, IsDeleted = false, CreatedAt = Seeded, UpdatedAt = Seeded
            }
        );
    }
}

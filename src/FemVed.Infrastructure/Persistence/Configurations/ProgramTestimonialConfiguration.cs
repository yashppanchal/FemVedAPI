using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramTestimonial"/> → <c>program_testimonials</c>.</summary>
internal sealed class ProgramTestimonialConfiguration : IEntityTypeConfiguration<ProgramTestimonial>
{
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramTestimonial> builder)
    {
        builder.ToTable("program_testimonials");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(t => t.ReviewerName).HasColumnName("reviewer_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.ReviewerTitle).HasColumnName("reviewer_title").HasMaxLength(200);
        builder.Property(t => t.ReviewText).HasColumnName("review_text").IsRequired();
        builder.Property(t => t.Rating).HasColumnName("rating").HasColumnType("smallint");
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(t => t.ProgramId).HasDatabaseName("idx_program_testimonials_program_id");

        builder.HasOne(t => t.Program)
            .WithMany(p => p.Testimonials)
            .HasForeignKey(t => t.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_testimonials_program");

        builder.HasData(
            new ProgramTestimonial
            {
                Id = Guid.Parse("ffffffff-0001-0000-0000-000000000001"),
                ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000001"),
                ReviewerName = "Riya S.", ReviewerTitle = "Marketing Professional, Mumbai",
                ReviewText = "After years of irregular cycles and unexplained fatigue, Dr. Prathima's program completely changed how I understand my own body. The Ayurvedic approach felt deeply personal and actually worked.",
                Rating = 5, IsActive = true, SortOrder = 1, CreatedAt = Seeded
            },
            new ProgramTestimonial
            {
                Id = Guid.Parse("ffffffff-0003-0000-0000-000000000001"),
                ProgramId = Guid.Parse("55555555-0000-0000-0000-000000000003"),
                ReviewerName = "Meera T.", ReviewerTitle = "Software Engineer, Bangalore",
                ReviewText = "The PCOS Reset program was the first time someone treated my PCOS as a metabolic condition and not just a hormonal one. Six months on and my cycles are regular for the first time in three years.",
                Rating = 5, IsActive = true, SortOrder = 1, CreatedAt = Seeded
            }
        );
    }
}

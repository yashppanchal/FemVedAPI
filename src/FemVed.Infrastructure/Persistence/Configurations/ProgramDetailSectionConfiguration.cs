using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for <see cref="ProgramDetailSection"/> → <c>program_detail_sections</c>.</summary>
internal sealed class ProgramDetailSectionConfiguration : IEntityTypeConfiguration<ProgramDetailSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProgramDetailSection> builder)
    {
        builder.ToTable("program_detail_sections");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(s => s.Heading).HasColumnName("heading").IsRequired();
        builder.Property(s => s.Description).HasColumnName("description").IsRequired();
        builder.Property(s => s.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(s => s.ProgramId).HasDatabaseName("idx_program_detail_sections_program_id");

        builder.HasOne(s => s.Program)
            .WithMany(p => p.DetailSections)
            .HasForeignKey(s => s.ProgramId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_program_detail_sections_program");
    }
}

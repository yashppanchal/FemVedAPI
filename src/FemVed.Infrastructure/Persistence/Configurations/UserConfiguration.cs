using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for the <see cref="User"/> entity → <c>users</c> table.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.CountryDialCode).HasColumnName("country_dial_code").HasMaxLength(10);
        builder.Property(u => u.CountryIsoCode).HasColumnName("country_iso_code").HasMaxLength(5);
        builder.Property(u => u.MobileNumber).HasColumnName("mobile_number").HasMaxLength(20);
        builder.Property(u => u.FullMobile).HasColumnName("full_mobile").HasMaxLength(30);
        builder.Property(u => u.IsMobileVerified).HasColumnName("is_mobile_verified").HasDefaultValue(false);
        builder.Property(u => u.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(false);
        builder.Property(u => u.WhatsAppOptIn).HasColumnName("whatsapp_opt_in").HasDefaultValue(false);
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("uq_users_email");
        builder.HasIndex(u => u.RoleId).HasDatabaseName("idx_users_role_id");
        builder.HasIndex(u => u.CountryIsoCode).HasDatabaseName("idx_users_iso_code");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .HasConstraintName("fk_users_role");
    }
}

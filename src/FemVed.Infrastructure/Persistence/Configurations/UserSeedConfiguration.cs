using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FemVed.Infrastructure.Persistence.Configurations;

/// <summary>
/// Applies seed data for expert User accounts.
/// Passwords are placeholder BCrypt hashes — must be reset via admin panel after first deploy.
/// </summary>
internal sealed class UserSeedConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly DateTimeOffset Seeded = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Note: UserConfiguration already configures table/columns.
        // This class only adds seed data via HasData.
        builder.HasData(
            new User
            {
                Id = Guid.Parse("33333333-0000-0000-0000-000000000001"),
                Email = "prathima@femved.com",
                PasswordHash = "$2a$12$PLACEHOLDER_HASH_PRATHIMA_CHANGE_ON_FIRST_LOGIN",
                RoleId = 2,
                FirstName = "Prathima", LastName = "Nagesh",
                CountryIsoCode = "IN", CountryDialCode = "+91",
                IsEmailVerified = true, IsActive = true, IsDeleted = false,
                IsMobileVerified = false, WhatsAppOptIn = false,
                CreatedAt = Seeded, UpdatedAt = Seeded
            },
            new User
            {
                Id = Guid.Parse("33333333-0000-0000-0000-000000000002"),
                Email = "kimberly@femved.com",
                PasswordHash = "$2a$12$PLACEHOLDER_HASH_KIMBERLY_CHANGE_ON_FIRST_LOGIN",
                RoleId = 2,
                FirstName = "Kimberly", LastName = "Parsons",
                CountryIsoCode = "GB", CountryDialCode = "+44",
                IsEmailVerified = true, IsActive = true, IsDeleted = false,
                IsMobileVerified = false, WhatsAppOptIn = false,
                CreatedAt = Seeded, UpdatedAt = Seeded
            }
        );
    }
}

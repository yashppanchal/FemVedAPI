using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FilterEmailUniqueIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the full unique constraint that covers ALL rows (including soft-deleted)
        migrationBuilder.Sql("ALTER TABLE users DROP CONSTRAINT uq_users_email;");

        // Create a partial unique index — only enforces uniqueness on non-deleted rows
        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX uq_users_email_active ON users(email) WHERE is_deleted = false;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS uq_users_email_active;");
        migrationBuilder.Sql("ALTER TABLE users ADD CONSTRAINT uq_users_email UNIQUE (email);");
    }
}

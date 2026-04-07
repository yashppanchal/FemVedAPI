using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MakePriceTierOptional : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE library_videos ALTER COLUMN price_tier_id DROP NOT NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE library_videos ALTER COLUMN price_tier_id SET NOT NULL;");
    }
}

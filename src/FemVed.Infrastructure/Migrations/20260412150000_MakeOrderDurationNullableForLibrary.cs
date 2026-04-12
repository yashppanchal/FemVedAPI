using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MakeOrderDurationNullableForLibrary : Migration
{
    /// <inheritdoc />
    /// <remarks>
    /// Library orders do not have a program duration — makes duration_id and duration_price_id
    /// nullable so that LIBRARY order_source rows can be inserted without violating the NOT NULL
    /// constraint that was set by the InitialCreate migration (which predated the library module).
    /// </remarks>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN duration_id DROP NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN duration_price_id DROP NOT NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN duration_id SET NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN duration_price_id SET NOT NULL;");
    }
}

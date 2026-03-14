using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddStartRequestAndEndDateToUserProgramAccess : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "requested_start_date",
            table: "user_program_access",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "start_request_status",
            table: "user_program_access",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "end_date",
            table: "user_program_access",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "requested_start_date", table: "user_program_access");
        migrationBuilder.DropColumn(name: "start_request_status", table: "user_program_access");
        migrationBuilder.DropColumn(name: "end_date", table: "user_program_access");
    }
}

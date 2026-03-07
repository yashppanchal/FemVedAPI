using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ended_by",
                table: "user_program_access",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ended_by_role",
                table: "user_program_access",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paused_at",
                table: "user_program_access",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gateway_response",
                table: "orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "program_session_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    access_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_session_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_psl_access",
                        column: x => x.access_id,
                        principalTable: "user_program_access",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_psl_access_id",
                table: "program_session_log",
                column: "access_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "program_session_log");

            migrationBuilder.DropColumn(
                name: "ended_by",
                table: "user_program_access");

            migrationBuilder.DropColumn(
                name: "ended_by_role",
                table: "user_program_access");

            migrationBuilder.DropColumn(
                name: "paused_at",
                table: "user_program_access");

            migrationBuilder.AlterColumn<string>(
                name: "gateway_response",
                table: "orders",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

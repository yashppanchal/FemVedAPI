using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionRateToExperts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commission_rate",
                table: "experts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 80.00m);

            migrationBuilder.CreateTable(
                name: "expert_payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    paid_by = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expert_payouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_expert_payouts_expert",
                        column: x => x.expert_id,
                        principalTable: "experts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expert_payouts_paid_by",
                        column: x => x.paid_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000001"),
                column: "commission_rate",
                value: 80.00m);

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000002"),
                column: "commission_rate",
                value: 80.00m);

            migrationBuilder.CreateIndex(
                name: "idx_expert_payouts_expert_id",
                table: "expert_payouts",
                column: "expert_id");

            migrationBuilder.CreateIndex(
                name: "idx_expert_payouts_paid_at",
                table: "expert_payouts",
                column: "paid_at");

            migrationBuilder.CreateIndex(
                name: "IX_expert_payouts_paid_by",
                table: "expert_payouts",
                column: "paid_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expert_payouts");

            migrationBuilder.DropColumn(
                name: "commission_rate",
                table: "experts");
        }
    }
}

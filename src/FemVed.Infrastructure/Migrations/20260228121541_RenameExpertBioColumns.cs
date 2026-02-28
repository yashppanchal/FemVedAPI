using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameExpertBioColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "short_bio",
                table: "experts",
                newName: "expert_grid_description");

            migrationBuilder.AddColumn<string>(
                name: "expert_detailed_description",
                table: "experts",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000001"),
                column: "expert_detailed_description",
                value: null);

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000002"),
                column: "expert_detailed_description",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expert_detailed_description",
                table: "experts");

            migrationBuilder.RenameColumn(
                name: "expert_grid_description",
                table: "experts",
                newName: "short_bio");
        }
    }
}

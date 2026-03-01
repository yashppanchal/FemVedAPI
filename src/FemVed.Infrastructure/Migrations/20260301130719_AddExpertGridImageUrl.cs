using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpertGridImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expert_grid_image_url",
                table: "experts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000001"),
                column: "expert_grid_image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "experts",
                keyColumn: "id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000002"),
                column: "expert_grid_image_url",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expert_grid_image_url",
                table: "experts");
        }
    }
}

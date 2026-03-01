using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToDomainsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "guided_domains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "guided_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "guided_categories",
                keyColumn: "id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000001"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "guided_categories",
                keyColumn: "id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "guided_categories",
                keyColumn: "id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000003"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "guided_categories",
                keyColumn: "id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000004"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "guided_domains",
                keyColumn: "id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"),
                columns: new string[0],
                values: new object[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "guided_domains");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "guided_categories");
        }
    }
}

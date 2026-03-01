using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponMinOrderAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "min_order_amount",
                table: "coupons",
                type: "numeric(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "min_order_amount",
                table: "coupons");
        }
    }
}

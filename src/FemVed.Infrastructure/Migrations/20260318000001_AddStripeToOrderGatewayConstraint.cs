using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FemVed.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddStripeToOrderGatewayConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE orders DROP CONSTRAINT chk_orders_gateway;");
        migrationBuilder.Sql("ALTER TABLE orders ADD CONSTRAINT chk_orders_gateway CHECK (payment_gateway IN ('CASHFREE','PAYPAL','STRIPE'));");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE orders DROP CONSTRAINT chk_orders_gateway;");
        migrationBuilder.Sql("ALTER TABLE orders ADD CONSTRAINT chk_orders_gateway CHECK (payment_gateway IN ('CASHFREE','PAYPAL'));");
    }
}

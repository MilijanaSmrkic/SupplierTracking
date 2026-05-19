using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplierTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_SupplierId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExpectedDeliveryDate",
                table: "Orders",
                column: "ExpectedDeliveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SupplierId_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "SupplierId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ExpectedDeliveryDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SupplierId_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SupplierId",
                table: "Orders",
                column: "SupplierId");
        }
    }
}

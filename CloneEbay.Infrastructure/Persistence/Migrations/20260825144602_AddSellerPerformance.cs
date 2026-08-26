using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "shippedAtUtc",
                table: "ShippingInfo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SellerPerformance",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sellerId = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    transactionDefectRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    lateShipmentRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    trackingUploadedOnTimeRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    casesClosedWithoutSellerResolutionRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    transactionsLast12Months = table.Column<int>(type: "int", nullable: false),
                    salesLast12Months = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    lastEvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nextEvaluationDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPerformance", x => x.id);
                    table.ForeignKey(
                        name: "FK_SellerPerformance_User_sellerId",
                        column: x => x.sellerId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPerformance_sellerId",
                table: "SellerPerformance",
                column: "sellerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerPerformance");

            migrationBuilder.DropColumn(
                name: "shippedAtUtc",
                table: "ShippingInfo");
        }
    }
}

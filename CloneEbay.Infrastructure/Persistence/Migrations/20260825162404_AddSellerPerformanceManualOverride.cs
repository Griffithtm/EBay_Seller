using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPerformanceManualOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "manualOverrideAtUtc",
                table: "SellerPerformance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "manualOverrideByAdminId",
                table: "SellerPerformance",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerPerformance_manualOverrideByAdminId",
                table: "SellerPerformance",
                column: "manualOverrideByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerPerformance_User_manualOverrideByAdminId",
                table: "SellerPerformance",
                column: "manualOverrideByAdminId",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerPerformance_User_manualOverrideByAdminId",
                table: "SellerPerformance");

            migrationBuilder.DropIndex(
                name: "IX_SellerPerformance_manualOverrideByAdminId",
                table: "SellerPerformance");

            migrationBuilder.DropColumn(
                name: "manualOverrideAtUtc",
                table: "SellerPerformance");

            migrationBuilder.DropColumn(
                name: "manualOverrideByAdminId",
                table: "SellerPerformance");
        }
    }
}

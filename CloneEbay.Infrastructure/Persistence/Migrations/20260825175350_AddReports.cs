using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportDownload",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    referenceCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    requestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fileUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    rangeStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rangeEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rangeTimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDownload", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReportDownload_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportSchedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lastRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nextRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    endDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    deliveryEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReportSchedule_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDownload_userId_referenceCode",
                table: "ReportDownload",
                columns: new[] { "userId", "referenceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportDownload_userId_requestedAtUtc",
                table: "ReportDownload",
                columns: new[] { "userId", "requestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedule_isActive_nextRunAtUtc",
                table: "ReportSchedule",
                columns: new[] { "isActive", "nextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedule_userId_source_type_isActive",
                table: "ReportSchedule",
                columns: new[] { "userId", "source", "type", "isActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportDownload");

            migrationBuilder.DropTable(
                name: "ReportSchedule");
        }
    }
}

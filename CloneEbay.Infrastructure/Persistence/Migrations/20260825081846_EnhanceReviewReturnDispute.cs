using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceReviewReturnDispute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "response",
                table: "Review",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ratingType",
                table: "Review",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "recipientId",
                table: "Review",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipientRole",
                table: "Review",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "repliedAt",
                table: "Review",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewerRole",
                table: "Review",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "revisionRequestedAt",
                table: "Review",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revisionStatus",
                table: "Review",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");



            migrationBuilder.AddColumn<DateTime>(
                name: "buyerShippedAt",
                table: "ReturnRequest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deliveredAt",
                table: "ReturnRequest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferredResolution",
                table: "ReturnRequest",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "refundAmount",
                table: "ReturnRequest",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "restockingFee",
                table: "ReturnRequest",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "returnCarrier",
                table: "ReturnRequest",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sellerNote",
                table: "ReturnRequest",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trackingNumber",
                table: "ReturnRequest",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedAt",
                table: "ReturnRequest",
                type: "datetime2",
                nullable: true);



            migrationBuilder.AddColumn<DateTime>(
                name: "createdAt",
                table: "Dispute",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedAt",
                table: "Dispute",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisputeEvidence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    disputeId = table.Column<int>(type: "int", nullable: false),
                    uploaderId = table.Column<int>(type: "int", nullable: false),
                    filePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    fileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    contentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeEvidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_Dispute_disputeId",
                        column: x => x.disputeId,
                        principalTable: "Dispute",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_User_uploaderId",
                        column: x => x.uploaderId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisputeMessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    disputeId = table.Column<int>(type: "int", nullable: false),
                    senderId = table.Column<int>(type: "int", nullable: false),
                    senderRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeMessage", x => x.id);
                    table.ForeignKey(
                        name: "FK_DisputeMessage_Dispute_disputeId",
                        column: x => x.disputeId,
                        principalTable: "Dispute",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisputeMessage_User_senderId",
                        column: x => x.senderId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnPolicy",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    storeId = table.Column<int>(type: "int", nullable: false),
                    acceptReturns = table.Column<bool>(type: "bit", nullable: false),
                    returnPeriodDays = table.Column<int>(type: "int", nullable: false),
                    returnShippingPaidBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnPolicy", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReturnPolicy_Store_storeId",
                        column: x => x.storeId,
                        principalTable: "Store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Review_recipientId",
                table: "Review",
                column: "recipientId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_disputeId",
                table: "DisputeEvidence",
                column: "disputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_uploaderId",
                table: "DisputeEvidence",
                column: "uploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessage_disputeId",
                table: "DisputeMessage",
                column: "disputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessage_senderId",
                table: "DisputeMessage",
                column: "senderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnPolicy_storeId",
                table: "ReturnPolicy",
                column: "storeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Review_User_recipientId",
                table: "Review",
                column: "recipientId",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Review_User_recipientId",
                table: "Review");

            migrationBuilder.DropTable(
                name: "DisputeEvidence");

            migrationBuilder.DropTable(
                name: "DisputeMessage");

            migrationBuilder.DropTable(
                name: "ReturnPolicy");

            migrationBuilder.DropIndex(
                name: "IX_Review_recipientId",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "ratingType",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "recipientId",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "recipientRole",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "repliedAt",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "reviewerRole",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "revisionRequestedAt",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "revisionStatus",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "buyerShippedAt",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "deliveredAt",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "preferredResolution",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "refundAmount",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "restockingFee",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "returnCarrier",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "sellerNote",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "trackingNumber",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "updatedAt",
                table: "ReturnRequest");

            migrationBuilder.DropColumn(
                name: "createdAt",
                table: "Dispute");

            migrationBuilder.DropColumn(
                name: "updatedAt",
                table: "Dispute");

            migrationBuilder.AlterColumn<string>(
                name: "response",
                table: "Review",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "ReturnRequest",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "Dispute",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}

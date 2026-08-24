using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModule7Messaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    avatarURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    emailConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    failedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    lockoutEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedProductId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_User_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_User_SenderId",
                        column: x => x.SenderId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    images = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    categoryId = table.Column<int>(type: "int", nullable: false),
                    sellerId = table.Column<int>(type: "int", nullable: false),
                    isAuction = table.Column<bool>(type: "bit", nullable: false),
                    auctionEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    auctionClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    auctionWinnerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.id);
                    table.CheckConstraint("CK_Product_AuctionEnd", "[isAuction] = 0 OR [auctionEndTime] IS NOT NULL");
                    table.CheckConstraint("CK_Product_Price_Positive", "[price] > 0");
                    table.ForeignKey(
                        name: "FK_Product_Category_categoryId",
                        column: x => x.categoryId,
                        principalTable: "Category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_User_auctionWinnerId",
                        column: x => x.auctionWinnerId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Product_User_sellerId",
                        column: x => x.sellerId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    tokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    expiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    replacedByTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sellerId = table.Column<int>(type: "int", nullable: false),
                    storeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    bannerImageURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    sellerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    legalName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    contactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    verificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    rejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    reviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.id);
                    table.ForeignKey(
                        name: "FK_Store_User_sellerId",
                        column: x => x.sellerId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bid",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    productId = table.Column<int>(type: "int", nullable: false),
                    bidderId = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bidTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bid", x => x.id);
                    table.CheckConstraint("CK_Bid_Amount_Positive", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_Bid_Product_productId",
                        column: x => x.productId,
                        principalTable: "Product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bid_User_bidderId",
                        column: x => x.bidderId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    productId = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    lastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.id);
                    table.CheckConstraint("CK_Inventory_Quantity_NonNegative", "[quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_Inventory_Product_productId",
                        column: x => x.productId,
                        principalTable: "Product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderTable",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    buyerId = table.Column<int>(type: "int", nullable: false),
                    addressId = table.Column<int>(type: "int", nullable: true),
                    orderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    totalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    auctionProductId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTable", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrderTable_Product_auctionProductId",
                        column: x => x.auctionProductId,
                        principalTable: "Product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTable_User_buyerId",
                        column: x => x.buyerId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    orderId = table.Column<int>(type: "int", nullable: false),
                    productId = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrderItem_OrderTable_orderId",
                        column: x => x.orderId,
                        principalTable: "OrderTable",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItem_Product_productId",
                        column: x => x.productId,
                        principalTable: "Product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bid_bidderId",
                table: "Bid",
                column: "bidderId");

            migrationBuilder.CreateIndex(
                name: "IX_Bid_productId_amount",
                table: "Bid",
                columns: new[] { "productId", "amount" });

            migrationBuilder.CreateIndex(
                name: "IX_Category_name",
                table: "Category",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_productId",
                table: "Inventory",
                column: "productId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId_IsRead",
                table: "Messages",
                columns: new[] { "ReceiverId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId_ReceiverId_Timestamp",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_orderId",
                table: "OrderItem",
                column: "orderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_productId",
                table: "OrderItem",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTable_auctionProductId",
                table: "OrderTable",
                column: "auctionProductId",
                unique: true,
                filter: "[auctionProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTable_buyerId",
                table: "OrderTable",
                column: "buyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_auctionEndTime",
                table: "Product",
                column: "auctionEndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Product_auctionWinnerId",
                table: "Product",
                column: "auctionWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_categoryId_status",
                table: "Product",
                columns: new[] { "categoryId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_sellerId_status",
                table: "Product",
                columns: new[] { "sellerId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_tokenHash",
                table: "RefreshToken",
                column: "tokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_userId_expiresAtUtc",
                table: "RefreshToken",
                columns: new[] { "userId", "expiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Store_sellerId",
                table: "Store",
                column: "sellerId",
                unique: true,
                filter: "[sellerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Store_storeName",
                table: "Store",
                column: "storeName",
                unique: true,
                filter: "[storeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Store_verificationStatus",
                table: "Store",
                column: "verificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_User_email",
                table: "User",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_username",
                table: "User",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bid");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "OrderTable");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}

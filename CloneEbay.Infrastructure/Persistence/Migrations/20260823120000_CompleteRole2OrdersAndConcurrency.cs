using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260823120000_CompleteRole2OrdersAndConcurrency")]
public sealed class CompleteRole2OrdersAndConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[OrderTable]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[OrderItem]', N'U') IS NULL
                THROW 51002, 'OrderTable and OrderItem from doc/clone_ebay_sqlserver_schema.sql are required.', 1;

            IF COL_LENGTH(N'dbo.OrderTable', N'auctionProductId') IS NULL
                ALTER TABLE [dbo].[OrderTable] ADD [auctionProductId] INT NULL;
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderTable_Product_auctionProductId')
                ALTER TABLE [dbo].[OrderTable] WITH CHECK ADD CONSTRAINT [FK_OrderTable_Product_auctionProductId]
                    FOREIGN KEY([auctionProductId]) REFERENCES [dbo].[Product]([id]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OrderTable_auctionProductId' AND object_id = OBJECT_ID(N'[dbo].[OrderTable]'))
                CREATE UNIQUE INDEX [UX_OrderTable_auctionProductId]
                    ON [dbo].[OrderTable]([auctionProductId]) WHERE [auctionProductId] IS NOT NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItem_productId' AND object_id = OBJECT_ID(N'[dbo].[OrderItem]'))
                CREATE INDEX [IX_OrderItem_productId] ON [dbo].[OrderItem]([productId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItem_productId' AND object_id = OBJECT_ID(N'[dbo].[OrderItem]'))
                DROP INDEX [IX_OrderItem_productId] ON [dbo].[OrderItem];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OrderTable_auctionProductId' AND object_id = OBJECT_ID(N'[dbo].[OrderTable]'))
                DROP INDEX [UX_OrderTable_auctionProductId] ON [dbo].[OrderTable];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderTable_Product_auctionProductId')
                ALTER TABLE [dbo].[OrderTable] DROP CONSTRAINT [FK_OrderTable_Product_auctionProductId];
            IF COL_LENGTH(N'dbo.OrderTable', N'auctionProductId') IS NOT NULL
                ALTER TABLE [dbo].[OrderTable] DROP COLUMN [auctionProductId];
            """);
    }
}

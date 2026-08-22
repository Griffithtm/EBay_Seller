using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260822220000_AddRole2Catalog")]
public sealed class AddRole2Catalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Category]', N'U') IS NULL OR
               OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL OR
               OBJECT_ID(N'[dbo].[Inventory]', N'U') IS NULL OR
               OBJECT_ID(N'[dbo].[Bid]', N'U') IS NULL
                THROW 51001, 'Apply doc/clone_ebay_sqlserver_schema.sql before the Role 2 migration.', 1;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[Category])
            BEGIN
                INSERT INTO [dbo].[Category] ([name]) VALUES
                    (N'Điện tử & Công nghệ'),
                    (N'Thời trang'),
                    (N'Nhà cửa & Đời sống'),
                    (N'Thể thao & Dã ngoại'),
                    (N'Sưu tầm & Nghệ thuật'),
                    (N'Phụ tùng & Xe cộ');
            END;

            UPDATE [dbo].[Category]
            SET [name] = CONCAT(N'Danh mục ', [id])
            WHERE [name] IS NULL OR LTRIM(RTRIM([name])) = '';

            UPDATE [dbo].[Product]
            SET [title] = CONCAT(N'Sản phẩm ', [id])
            WHERE [title] IS NULL OR LTRIM(RTRIM([title])) = '';
            UPDATE [dbo].[Product] SET [description] = N'' WHERE [description] IS NULL;
            UPDATE [dbo].[Product] SET [price] = 0.01 WHERE [price] IS NULL OR [price] <= 0;
            UPDATE [dbo].[Product] SET [images] = N'[]' WHERE [images] IS NULL OR LTRIM(RTRIM([images])) = '';
            UPDATE [dbo].[Product] SET [isAuction] = 0 WHERE [isAuction] IS NULL;

            IF COL_LENGTH(N'dbo.Product', N'status') IS NULL
                ALTER TABLE [dbo].[Product] ADD [status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Product_status] DEFAULT('Active');
            IF COL_LENGTH(N'dbo.Product', N'createdAtUtc') IS NULL
                ALTER TABLE [dbo].[Product] ADD [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Product_createdAtUtc] DEFAULT(SYSUTCDATETIME());
            IF COL_LENGTH(N'dbo.Product', N'updatedAtUtc') IS NULL
                ALTER TABLE [dbo].[Product] ADD [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Product_updatedAtUtc] DEFAULT(SYSUTCDATETIME());
            IF COL_LENGTH(N'dbo.Product', N'auctionClosedAtUtc') IS NULL
                ALTER TABLE [dbo].[Product] ADD [auctionClosedAtUtc] DATETIME2 NULL;
            IF COL_LENGTH(N'dbo.Product', N'auctionWinnerId') IS NULL
                ALTER TABLE [dbo].[Product] ADD [auctionWinnerId] INT NULL;

            """);

        // SQL Server resolves column names when compiling a batch. Statements that use
        // Role 2 columns must run after the ALTER TABLE batch above has completed.
        migrationBuilder.Sql("""

            UPDATE [dbo].[Inventory] SET [quantity] = 0 WHERE [quantity] IS NULL OR [quantity] < 0;
            UPDATE [dbo].[Inventory] SET [lastUpdated] = SYSUTCDATETIME() WHERE [lastUpdated] IS NULL;
            UPDATE productRow
            SET [status] = 'OutOfStock'
            FROM [dbo].[Product] productRow
            INNER JOIN [dbo].[Inventory] inventoryRow ON inventoryRow.[productId] = productRow.[id]
            WHERE inventoryRow.[quantity] = 0 AND productRow.[status] = 'Active';

            UPDATE [dbo].[Bid] SET [amount] = 0.01 WHERE [amount] IS NULL OR [amount] <= 0;
            UPDATE [dbo].[Bid] SET [bidTime] = SYSUTCDATETIME() WHERE [bidTime] IS NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_User_auctionWinnerId')
                ALTER TABLE [dbo].[Product] WITH CHECK ADD CONSTRAINT [FK_Product_User_auctionWinnerId]
                    FOREIGN KEY([auctionWinnerId]) REFERENCES [dbo].[User]([id]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_name' AND object_id = OBJECT_ID(N'[dbo].[Category]'))
                CREATE UNIQUE INDEX [IX_Category_name] ON [dbo].[Category]([name]) WHERE [name] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_sellerId_status' AND object_id = OBJECT_ID(N'[dbo].[Product]'))
                CREATE INDEX [IX_Product_sellerId_status] ON [dbo].[Product]([sellerId], [status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_categoryId_status' AND object_id = OBJECT_ID(N'[dbo].[Product]'))
                CREATE INDEX [IX_Product_categoryId_status] ON [dbo].[Product]([categoryId], [status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_auctionEndTime' AND object_id = OBJECT_ID(N'[dbo].[Product]'))
                CREATE INDEX [IX_Product_auctionEndTime] ON [dbo].[Product]([auctionEndTime]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Inventory_productId' AND object_id = OBJECT_ID(N'[dbo].[Inventory]'))
                CREATE UNIQUE INDEX [IX_Inventory_productId] ON [dbo].[Inventory]([productId]) WHERE [productId] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bid_productId_amount' AND object_id = OBJECT_ID(N'[dbo].[Bid]'))
                CREATE INDEX [IX_Bid_productId_amount] ON [dbo].[Bid]([productId], [amount] DESC);

            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_Price_Positive')
                ALTER TABLE [dbo].[Product] ADD CONSTRAINT [CK_Product_Price_Positive] CHECK ([price] > 0);
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_AuctionEnd')
                ALTER TABLE [dbo].[Product] ADD CONSTRAINT [CK_Product_AuctionEnd] CHECK ([isAuction] = 0 OR [auctionEndTime] IS NOT NULL);
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Inventory_Quantity_NonNegative')
                ALTER TABLE [dbo].[Inventory] ADD CONSTRAINT [CK_Inventory_Quantity_NonNegative] CHECK ([quantity] >= 0);
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Bid_Amount_Positive')
                ALTER TABLE [dbo].[Bid] ADD CONSTRAINT [CK_Bid_Amount_Positive] CHECK ([amount] > 0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Bid_Amount_Positive') ALTER TABLE [dbo].[Bid] DROP CONSTRAINT [CK_Bid_Amount_Positive];
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Inventory_Quantity_NonNegative') ALTER TABLE [dbo].[Inventory] DROP CONSTRAINT [CK_Inventory_Quantity_NonNegative];
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_AuctionEnd') ALTER TABLE [dbo].[Product] DROP CONSTRAINT [CK_Product_AuctionEnd];
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Product_Price_Positive') ALTER TABLE [dbo].[Product] DROP CONSTRAINT [CK_Product_Price_Positive];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bid_productId_amount' AND object_id = OBJECT_ID(N'[dbo].[Bid]')) DROP INDEX [IX_Bid_productId_amount] ON [dbo].[Bid];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Inventory_productId' AND object_id = OBJECT_ID(N'[dbo].[Inventory]')) DROP INDEX [IX_Inventory_productId] ON [dbo].[Inventory];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_auctionEndTime' AND object_id = OBJECT_ID(N'[dbo].[Product]')) DROP INDEX [IX_Product_auctionEndTime] ON [dbo].[Product];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_categoryId_status' AND object_id = OBJECT_ID(N'[dbo].[Product]')) DROP INDEX [IX_Product_categoryId_status] ON [dbo].[Product];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_sellerId_status' AND object_id = OBJECT_ID(N'[dbo].[Product]')) DROP INDEX [IX_Product_sellerId_status] ON [dbo].[Product];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_name' AND object_id = OBJECT_ID(N'[dbo].[Category]')) DROP INDEX [IX_Category_name] ON [dbo].[Category];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_User_auctionWinnerId') ALTER TABLE [dbo].[Product] DROP CONSTRAINT [FK_Product_User_auctionWinnerId];
            IF COL_LENGTH(N'dbo.Product', N'auctionWinnerId') IS NOT NULL ALTER TABLE [dbo].[Product] DROP COLUMN [auctionWinnerId];
            IF COL_LENGTH(N'dbo.Product', N'auctionClosedAtUtc') IS NOT NULL ALTER TABLE [dbo].[Product] DROP COLUMN [auctionClosedAtUtc];
            IF COL_LENGTH(N'dbo.Product', N'updatedAtUtc') IS NOT NULL ALTER TABLE [dbo].[Product] DROP CONSTRAINT [DF_Product_updatedAtUtc], COLUMN [updatedAtUtc];
            IF COL_LENGTH(N'dbo.Product', N'createdAtUtc') IS NOT NULL ALTER TABLE [dbo].[Product] DROP CONSTRAINT [DF_Product_createdAtUtc], COLUMN [createdAtUtc];
            IF COL_LENGTH(N'dbo.Product', N'status') IS NOT NULL ALTER TABLE [dbo].[Product] DROP CONSTRAINT [DF_Product_status], COLUMN [status];
            """);
    }
}

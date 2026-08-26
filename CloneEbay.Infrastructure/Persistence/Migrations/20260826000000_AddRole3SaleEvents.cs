using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

/// <summary>
/// Role 3: Sale Events (kept separate from the existing AddRole3Coupon migration/feature on this
/// branch — this migration only introduces SaleEvent/SaleEventTier/SaleEventProduct and does not
/// touch the [Coupon] table at all). Idempotent, matching the Role 1/2 migrations' convention.
/// </summary>
[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260826000000_AddRole3SaleEvents")]
public sealed class AddRole3SaleEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[User]', N'U') IS NULL
                THROW 51001, 'Apply doc/clone_ebay_sqlserver_schema.sql and the Role 1/2 migrations before the Role 3 Sale Events migration.', 1;

            IF OBJECT_ID(N'[dbo].[SaleEvent]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SaleEvent]
                (
                    [id] INT IDENTITY(1,1) CONSTRAINT [PK_SaleEvent] PRIMARY KEY,
                    [sellerId] INT NOT NULL,
                    [name] NVARCHAR(90) NOT NULL,
                    [description] NVARCHAR(255) NULL,
                    [mode] NVARCHAR(30) NOT NULL,
                    [status] NVARCHAR(20) NOT NULL,
                    [startDateUtc] DATETIME2 NOT NULL,
                    [endDateUtc] DATETIME2 NOT NULL,
                    [offerFreeShipping] BIT NOT NULL,
                    [includeSkippedItems] BIT NOT NULL,
                    [blockPriceIncreaseRevisions] BIT NOT NULL,
                    [highlightPercentage] DECIMAL(5,2) NULL,
                    [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_SaleEvent_createdAtUtc] DEFAULT(SYSUTCDATETIME()),
                    [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_SaleEvent_updatedAtUtc] DEFAULT(SYSUTCDATETIME()),
                    CONSTRAINT [FK_SaleEvent_User_sellerId] FOREIGN KEY ([sellerId]) REFERENCES [dbo].[User]([id])
                );
                CREATE UNIQUE INDEX [UX_SaleEvent_sellerId_name] ON [dbo].[SaleEvent]([sellerId], [name]);
            END;

            IF OBJECT_ID(N'[dbo].[SaleEventTier]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SaleEventTier]
                (
                    [id] INT IDENTITY(1,1) CONSTRAINT [PK_SaleEventTier] PRIMARY KEY,
                    [saleEventId] INT NOT NULL,
                    [discountType] NVARCHAR(10) NOT NULL,
                    [discountValue] DECIMAL(10,2) NOT NULL,
                    [priority] INT NOT NULL,
                    [label] NVARCHAR(100) NULL,
                    CONSTRAINT [FK_SaleEventTier_SaleEvent_saleEventId] FOREIGN KEY ([saleEventId]) REFERENCES [dbo].[SaleEvent]([id])
                );
                CREATE INDEX [IX_SaleEventTier_saleEventId] ON [dbo].[SaleEventTier]([saleEventId]);
            END;

            IF OBJECT_ID(N'[dbo].[SaleEventProduct]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SaleEventProduct]
                (
                    [id] INT IDENTITY(1,1) CONSTRAINT [PK_SaleEventProduct] PRIMARY KEY,
                    [saleEventId] INT NOT NULL,
                    [tierId] INT NOT NULL,
                    [productId] INT NOT NULL,
                    CONSTRAINT [FK_SaleEventProduct_SaleEvent_saleEventId] FOREIGN KEY ([saleEventId]) REFERENCES [dbo].[SaleEvent]([id]),
                    CONSTRAINT [FK_SaleEventProduct_SaleEventTier_tierId] FOREIGN KEY ([tierId]) REFERENCES [dbo].[SaleEventTier]([id]),
                    CONSTRAINT [FK_SaleEventProduct_Product_productId] FOREIGN KEY ([productId]) REFERENCES [dbo].[Product]([id])
                );
                CREATE UNIQUE INDEX [UX_SaleEventProduct_saleEventId_productId] ON [dbo].[SaleEventProduct]([saleEventId], [productId]);
                CREATE INDEX [IX_SaleEventProduct_tierId] ON [dbo].[SaleEventProduct]([tierId]);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[SaleEventProduct]', N'U') IS NOT NULL DROP TABLE [dbo].[SaleEventProduct];
            IF OBJECT_ID(N'[dbo].[SaleEventTier]', N'U') IS NOT NULL DROP TABLE [dbo].[SaleEventTier];
            IF OBJECT_ID(N'[dbo].[SaleEvent]', N'U') IS NOT NULL DROP TABLE [dbo].[SaleEvent];
            """);
    }
}

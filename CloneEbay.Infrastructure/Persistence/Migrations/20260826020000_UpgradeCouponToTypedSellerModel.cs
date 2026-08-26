using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

/// <summary>
/// Upgrades the Role 3 Coupon feature (added by 20260824090000_AddRole3Coupon: one discount
/// percent tied to exactly one product) into a full, seller-owned, typed coupon system with 11
/// built-in coupon types and per-type conditions. Additive and idempotent: every pre-existing
/// column (code, discountPercent, startDate, endDate, maxUsage, usageCount, productId,
/// createdAtUtc, updatedAtUtc) is kept as-is, and existing rows are backfilled so they keep
/// working under the new model (ExtraPercentOff, owned by the seller of their original product).
/// </summary>
[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260826020000_UpgradeCouponToTypedSellerModel")]
public sealed class UpgradeCouponToTypedSellerModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Coupon]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[User]', N'U') IS NULL OR OBJECT_ID(N'[dbo].[Category]', N'U') IS NULL
                THROW 51003, 'Apply the Role 1/2/3 Coupon migrations before the coupon-upgrade migration.', 1;

            IF OBJECT_ID(N'[dbo].[CouponType]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CouponType]
                (
                    [id] INT IDENTITY(1,1) CONSTRAINT [PK_CouponType] PRIMARY KEY,
                    [name] NVARCHAR(100) NOT NULL,
                    [description] NVARCHAR(255) NULL,
                    [isActive] BIT NOT NULL CONSTRAINT [DF_CouponType_isActive] DEFAULT(1)
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[CouponType])
            BEGIN
                SET IDENTITY_INSERT [dbo].[CouponType] ON;
                INSERT INTO [dbo].[CouponType] ([id], [name], [description], [isActive]) VALUES
                    (1, N'Extra percent off', N'A flat percentage off the item price.', 1),
                    (2, N'Extra percent off Y+ items', N'Extra percentage off when the buyer takes a minimum quantity.', 1),
                    (3, N'Extra amount off amount or more', N'A flat amount off once the order reaches a minimum value.', 1),
                    (4, N'Buy X get Y at percent off', N'Buy a quantity, get another quantity at a percentage discount.', 1),
                    (5, N'Buy X get Y free', N'Buy a quantity, get another quantity for free.', 1),
                    (6, N'Extra percent off amount or more', N'Extra percentage off once the order reaches a minimum value.', 1),
                    (7, N'Extra amount off items threshold', N'A flat amount off once a minimum item count is reached.', 1),
                    (8, N'Extra amount off', N'A flat amount off the order.', 1),
                    (9, N'Extra amount off each item', N'A flat amount off every qualifying item.', 1),
                    (10, N'Save amount for every N items', N'A flat amount saved for every N items purchased.', 1),
                    (11, N'Save amount for every amount spent', N'A flat amount saved for every threshold of spend reached.', 1);
                SET IDENTITY_INSERT [dbo].[CouponType] OFF;
            END;

            IF OBJECT_ID(N'[dbo].[CouponCondition]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CouponCondition]
                (
                    [id] INT IDENTITY(1,1) CONSTRAINT [PK_CouponCondition] PRIMARY KEY,
                    [couponId] INT NOT NULL,
                    [buyQuantity] INT NULL,
                    [getQuantity] INT NULL,
                    [getDiscountPercent] DECIMAL(5,2) NULL,
                    [saveEveryAmount] DECIMAL(10,2) NULL,
                    [saveEveryItems] INT NULL,
                    [conditionDescription] NVARCHAR(255) NULL,
                    CONSTRAINT [FK_CouponCondition_Coupon_couponId] FOREIGN KEY ([couponId]) REFERENCES [dbo].[Coupon]([id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_CouponCondition_couponId] ON [dbo].[CouponCondition]([couponId]);
            END;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Coupon', N'couponTypeId') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [couponTypeId] INT NOT NULL CONSTRAINT [DF_Coupon_couponTypeId] DEFAULT(1);
            IF COL_LENGTH(N'dbo.Coupon', N'categoryId') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [categoryId] INT NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'sellerId') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [sellerId] INT NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'name') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [name] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Coupon_name] DEFAULT(N'');
            IF COL_LENGTH(N'dbo.Coupon', N'discountValue') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [discountValue] DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Coupon_discountValue] DEFAULT(0);
            IF COL_LENGTH(N'dbo.Coupon', N'discountUnit') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [discountUnit] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Coupon_discountUnit] DEFAULT('Percent');
            IF COL_LENGTH(N'dbo.Coupon', N'maxDiscount') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [maxDiscount] DECIMAL(10,2) NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'usagePerUser') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [usagePerUser] INT NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'minimumOrderValue') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [minimumOrderValue] DECIMAL(10,2) NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'applicablePriceMin') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [applicablePriceMin] DECIMAL(10,2) NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'applicablePriceMax') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [applicablePriceMax] DECIMAL(10,2) NULL;
            IF COL_LENGTH(N'dbo.Coupon', N'isActive') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [isActive] BIT NOT NULL CONSTRAINT [DF_Coupon_isActive] DEFAULT(1);
            """);

        // Backfill legacy rows (created under the old one-product/one-percent model) so they
        // keep working under the new model: name from code, discountValue from discountPercent,
        // sellerId derived from the product they were originally scoped to. Must be its own batch
        // — SQL Server resolves column names when it compiles a batch, so referencing a column
        // added by the ALTER TABLE above in the same batch would fail to bind.
        migrationBuilder.Sql("""
            UPDATE c
            SET c.[name] = c.[code],
                c.[discountValue] = ISNULL(c.[discountPercent], 0),
                c.[discountUnit] = 'Percent',
                c.[couponTypeId] = 1,
                c.[sellerId] = p.[sellerId]
            FROM [dbo].[Coupon] c
            LEFT JOIN [dbo].[Product] p ON p.[id] = c.[productId]
            WHERE c.[name] = N'';

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_CouponType_couponTypeId')
                ALTER TABLE [dbo].[Coupon] WITH CHECK ADD CONSTRAINT [FK_Coupon_CouponType_couponTypeId]
                    FOREIGN KEY([couponTypeId]) REFERENCES [dbo].[CouponType]([id]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_User_sellerId')
                ALTER TABLE [dbo].[Coupon] WITH CHECK ADD CONSTRAINT [FK_Coupon_User_sellerId]
                    FOREIGN KEY([sellerId]) REFERENCES [dbo].[User]([id]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_Category_categoryId')
                ALTER TABLE [dbo].[Coupon] WITH CHECK ADD CONSTRAINT [FK_Coupon_Category_categoryId]
                    FOREIGN KEY([categoryId]) REFERENCES [dbo].[Category]([id]);

            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_productId_code' AND object_id = OBJECT_ID(N'[dbo].[Coupon]'))
                DROP INDEX [IX_Coupon_productId_code] ON [dbo].[Coupon];

            -- Codes used to only need to be unique per product, so legacy rows can legitimately
            -- share a code across different products. A hard UNIQUE index would abort this whole
            -- migration in that case, so only add it when the existing data already satisfies a
            -- global uniqueness rule; otherwise fall back to a regular (non-unique) index. New
            -- coupons created through CouponService still get global-uniqueness enforced at the
            -- application layer (ICouponRepository.CodeExistsAsync) regardless of which index
            -- shape exists here.
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name IN (N'UX_Coupon_code', N'IX_Coupon_code') AND object_id = OBJECT_ID(N'[dbo].[Coupon]'))
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[Coupon] GROUP BY [code] HAVING COUNT(*) > 1)
                    CREATE INDEX [IX_Coupon_code] ON [dbo].[Coupon]([code]);
                ELSE
                    CREATE UNIQUE INDEX [UX_Coupon_code] ON [dbo].[Coupon]([code]);
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_sellerId' AND object_id = OBJECT_ID(N'[dbo].[Coupon]'))
                CREATE INDEX [IX_Coupon_sellerId] ON [dbo].[Coupon]([sellerId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_sellerId' AND object_id = OBJECT_ID(N'[dbo].[Coupon]')) DROP INDEX [IX_Coupon_sellerId] ON [dbo].[Coupon];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Coupon_code' AND object_id = OBJECT_ID(N'[dbo].[Coupon]')) DROP INDEX [UX_Coupon_code] ON [dbo].[Coupon];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_code' AND object_id = OBJECT_ID(N'[dbo].[Coupon]')) DROP INDEX [IX_Coupon_code] ON [dbo].[Coupon];
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_productId_code' AND object_id = OBJECT_ID(N'[dbo].[Coupon]'))
                CREATE UNIQUE INDEX [IX_Coupon_productId_code] ON [dbo].[Coupon]([productId], [code]);

            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_Category_categoryId') ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [FK_Coupon_Category_categoryId];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_User_sellerId') ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [FK_Coupon_User_sellerId];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Coupon_CouponType_couponTypeId') ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [FK_Coupon_CouponType_couponTypeId];
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Coupon', N'isActive') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [DF_Coupon_isActive], COLUMN [isActive];
            IF COL_LENGTH(N'dbo.Coupon', N'applicablePriceMax') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [applicablePriceMax];
            IF COL_LENGTH(N'dbo.Coupon', N'applicablePriceMin') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [applicablePriceMin];
            IF COL_LENGTH(N'dbo.Coupon', N'minimumOrderValue') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [minimumOrderValue];
            IF COL_LENGTH(N'dbo.Coupon', N'usagePerUser') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [usagePerUser];
            IF COL_LENGTH(N'dbo.Coupon', N'maxDiscount') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [maxDiscount];
            IF COL_LENGTH(N'dbo.Coupon', N'discountUnit') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [DF_Coupon_discountUnit], COLUMN [discountUnit];
            IF COL_LENGTH(N'dbo.Coupon', N'discountValue') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [DF_Coupon_discountValue], COLUMN [discountValue];
            IF COL_LENGTH(N'dbo.Coupon', N'name') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [DF_Coupon_name], COLUMN [name];
            IF COL_LENGTH(N'dbo.Coupon', N'sellerId') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [sellerId];
            IF COL_LENGTH(N'dbo.Coupon', N'categoryId') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP COLUMN [categoryId];
            IF COL_LENGTH(N'dbo.Coupon', N'couponTypeId') IS NOT NULL ALTER TABLE [dbo].[Coupon] DROP CONSTRAINT [DF_Coupon_couponTypeId], COLUMN [couponTypeId];
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[CouponCondition]', N'U') IS NOT NULL DROP TABLE [dbo].[CouponCondition];
            IF OBJECT_ID(N'[dbo].[CouponType]', N'U') IS NOT NULL DROP TABLE [dbo].[CouponType];
            """);
    }
}

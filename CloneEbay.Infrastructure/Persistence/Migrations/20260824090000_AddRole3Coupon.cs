using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260824090000_AddRole3Coupon")]
public sealed class AddRole3Coupon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL
                THROW 51002, 'Apply the Role 2 catalog migration before the Role 3 coupon migration.', 1;

            IF OBJECT_ID(N'[dbo].[Coupon]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Coupon] (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Coupon] PRIMARY KEY,
                    [code] NVARCHAR(50) NOT NULL,
                    [discountPercent] DECIMAL(5,2) NOT NULL,
                    [startDate] DATETIME2 NOT NULL,
                    [endDate] DATETIME2 NOT NULL,
                    [maxUsage] INT NOT NULL,
                    [usageCount] INT NOT NULL CONSTRAINT [DF_Coupon_usageCount] DEFAULT(0),
                    [productId] INT NOT NULL,
                    [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Coupon_createdAtUtc] DEFAULT(SYSUTCDATETIME()),
                    [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Coupon_updatedAtUtc] DEFAULT(SYSUTCDATETIME()),
                    CONSTRAINT [FK_Coupon_Product_productId] FOREIGN KEY ([productId]) REFERENCES [dbo].[Product]([id]) ON DELETE CASCADE,
                    CONSTRAINT [CK_Coupon_DiscountPercent] CHECK ([discountPercent] > 0 AND [discountPercent] <= 100),
                    CONSTRAINT [CK_Coupon_DateRange] CHECK ([endDate] > [startDate]),
                    CONSTRAINT [CK_Coupon_MaxUsage] CHECK ([maxUsage] >= 1),
                    CONSTRAINT [CK_Coupon_UsageCount] CHECK ([usageCount] >= 0)
                );
            END;

            -- The reference schema (doc/clone_ebay_sqlserver_schema.sql) ships an older Coupon
            -- table without usage tracking or audit columns; backfill them additively so an
            -- existing database can move onto the Role 3 module without data loss.
            IF COL_LENGTH(N'dbo.Coupon', N'usageCount') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [usageCount] INT NOT NULL CONSTRAINT [DF_Coupon_usageCount] DEFAULT(0);
            IF COL_LENGTH(N'dbo.Coupon', N'createdAtUtc') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Coupon_createdAtUtc] DEFAULT(SYSUTCDATETIME());
            IF COL_LENGTH(N'dbo.Coupon', N'updatedAtUtc') IS NULL
                ALTER TABLE [dbo].[Coupon] ADD [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Coupon_updatedAtUtc] DEFAULT(SYSUTCDATETIME());

            UPDATE [dbo].[Coupon] SET [code] = CONCAT(N'COUPON-', [id]) WHERE [code] IS NULL OR LTRIM(RTRIM([code])) = '';
            UPDATE [dbo].[Coupon] SET [discountPercent] = 1 WHERE [discountPercent] IS NULL OR [discountPercent] <= 0;
            UPDATE [dbo].[Coupon] SET [maxUsage] = 1 WHERE [maxUsage] IS NULL OR [maxUsage] < 1;
            UPDATE [dbo].[Coupon] SET [startDate] = SYSUTCDATETIME() WHERE [startDate] IS NULL;
            UPDATE [dbo].[Coupon] SET [endDate] = DATEADD(DAY, 30, [startDate]) WHERE [endDate] IS NULL OR [endDate] <= [startDate];

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Coupon_productId_code' AND object_id = OBJECT_ID(N'[dbo].[Coupon]'))
                CREATE UNIQUE INDEX [IX_Coupon_productId_code] ON [dbo].[Coupon]([productId], [code]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[Coupon];");
    }
}

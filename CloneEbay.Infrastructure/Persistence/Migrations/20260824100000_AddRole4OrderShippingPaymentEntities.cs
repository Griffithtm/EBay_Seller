using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260824100000_AddRole4OrderShippingPaymentEntities")]
public sealed class AddRole4OrderShippingPaymentEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Address]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Address] (
                    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [userId] INT NOT NULL,
                    [fullName] NVARCHAR(100) NULL,
                    [phone] NVARCHAR(20) NULL,
                    [street] NVARCHAR(100) NULL,
                    [city] NVARCHAR(50) NULL,
                    [state] NVARCHAR(50) NULL,
                    [country] NVARCHAR(50) NULL,
                    [isDefault] BIT NULL,
                    CONSTRAINT [FK_Address_User_userId] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'[dbo].[Payment]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Payment] (
                    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [orderId] INT NOT NULL,
                    [userId] INT NOT NULL,
                    [amount] DECIMAL(18,2) NOT NULL,
                    [method] NVARCHAR(50) NULL,
                    [status] NVARCHAR(20) NULL,
                    [paidAt] DATETIME NULL,
                    CONSTRAINT [FK_Payment_OrderTable_orderId] FOREIGN KEY ([orderId]) REFERENCES [dbo].[OrderTable] ([id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Payment_User_userId] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
                );
            END;

            IF OBJECT_ID(N'[dbo].[ShippingInfo]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ShippingInfo] (
                    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [orderId] INT NOT NULL,
                    [carrier] NVARCHAR(100) NULL,
                    [trackingNumber] NVARCHAR(100) NULL,
                    [status] NVARCHAR(50) NULL,
                    [estimatedArrival] DATETIME NULL,
                    CONSTRAINT [FK_ShippingInfo_OrderTable_orderId] FOREIGN KEY ([orderId]) REFERENCES [dbo].[OrderTable] ([id]) ON DELETE CASCADE
                );
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[ShippingInfo]', N'U') IS NOT NULL DROP TABLE [dbo].[ShippingInfo];
            IF OBJECT_ID(N'[dbo].[Payment]', N'U') IS NOT NULL DROP TABLE [dbo].[Payment];
            IF OBJECT_ID(N'[dbo].[Address]', N'U') IS NOT NULL DROP TABLE [dbo].[Address];
            """);
    }
}

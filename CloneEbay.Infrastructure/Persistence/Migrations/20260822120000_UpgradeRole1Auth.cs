using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260822120000_UpgradeRole1Auth")]
public sealed class UpgradeRole1Auth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[User]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[User] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [username] NVARCHAR(100),
                    [email] NVARCHAR(100) UNIQUE,
                    [password] NVARCHAR(255),
                    [role] NVARCHAR(20),
                    [avatarURL] NVARCHAR(MAX)
                );
            END;

            IF OBJECT_ID(N'[dbo].[Category]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Category] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [name] NVARCHAR(100)
                );
            END;

            IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Product] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [title] NVARCHAR(255),
                    [description] NVARCHAR(MAX),
                    [price] DECIMAL(10,2),
                    [images] NVARCHAR(MAX),
                    [categoryId] INT FOREIGN KEY REFERENCES [Category](id),
                    [sellerId] INT FOREIGN KEY REFERENCES [User](id),
                    [isAuction] BIT,
                    [auctionEndTime] DATETIME
                );
            END;

            IF OBJECT_ID(N'[dbo].[OrderTable]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OrderTable] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [buyerId] INT FOREIGN KEY REFERENCES [User](id),
                    [addressId] INT,
                    [orderDate] DATETIME,
                    [totalPrice] DECIMAL(10,2),
                    [status] NVARCHAR(20)
                );
            END;

            IF OBJECT_ID(N'[dbo].[OrderItem]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OrderItem] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
                    [productId] INT FOREIGN KEY REFERENCES [Product](id),
                    [quantity] INT,
                    [unitPrice] DECIMAL(10,2)
                );
            END;

            IF OBJECT_ID(N'[dbo].[Bid]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Bid] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [productId] INT FOREIGN KEY REFERENCES [Product](id),
                    [bidderId] INT FOREIGN KEY REFERENCES [User](id),
                    [amount] DECIMAL(10,2),
                    [bidTime] DATETIME
                );
            END;

            IF OBJECT_ID(N'[dbo].[Inventory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Inventory] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [productId] INT FOREIGN KEY REFERENCES [Product](id),
                    [quantity] INT,
                    [lastUpdated] DATETIME
                );
            END;

            IF OBJECT_ID(N'[dbo].[Store]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Store] (
                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                    [sellerId] INT FOREIGN KEY REFERENCES [User](id),
                    [storeName] NVARCHAR(100),
                    [description] NVARCHAR(MAX),
                    [bannerImageURL] NVARCHAR(MAX)
                );
            END;

            UPDATE [dbo].[User]
            SET [username] = CONCAT('legacy-user-', [id])
            WHERE [username] IS NULL OR LTRIM(RTRIM([username])) = '';

            UPDATE [dbo].[User]
            SET [email] = CONCAT('legacy-user-', [id], '@cloneebay.local')
            WHERE [email] IS NULL OR LTRIM(RTRIM([email])) = '';

            UPDATE [dbo].[User]
            SET [password] = 'LEGACY_PASSWORD_RESET_REQUIRED'
            WHERE [password] IS NULL OR LTRIM(RTRIM([password])) = '';

            UPDATE [dbo].[User]
            SET [role] = 'Buyer'
            WHERE [role] IS NULL OR [role] NOT IN ('Buyer', 'Seller', 'Admin');

            IF COL_LENGTH(N'dbo.User', N'emailConfirmed') IS NULL
                ALTER TABLE [dbo].[User] ADD [emailConfirmed] BIT NOT NULL CONSTRAINT [DF_User_emailConfirmed] DEFAULT(0);
            IF COL_LENGTH(N'dbo.User', N'isActive') IS NULL
                ALTER TABLE [dbo].[User] ADD [isActive] BIT NOT NULL CONSTRAINT [DF_User_isActive] DEFAULT(1);
            IF COL_LENGTH(N'dbo.User', N'failedLoginAttempts') IS NULL
                ALTER TABLE [dbo].[User] ADD [failedLoginAttempts] INT NOT NULL CONSTRAINT [DF_User_failedLoginAttempts] DEFAULT(0);
            IF COL_LENGTH(N'dbo.User', N'lockoutEndUtc') IS NULL
                ALTER TABLE [dbo].[User] ADD [lockoutEndUtc] DATETIME2 NULL;
            IF COL_LENGTH(N'dbo.User', N'createdAtUtc') IS NULL
                ALTER TABLE [dbo].[User] ADD [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_User_createdAtUtc] DEFAULT(SYSUTCDATETIME());
            IF COL_LENGTH(N'dbo.User', N'updatedAtUtc') IS NULL
                ALTER TABLE [dbo].[User] ADD [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_User_updatedAtUtc] DEFAULT(SYSUTCDATETIME());

            UPDATE [dbo].[Store]
            SET [storeName] = CONCAT('Store-', [id])
            WHERE [storeName] IS NULL OR LTRIM(RTRIM([storeName])) = '';
            IF COL_LENGTH(N'dbo.Store', N'sellerType') IS NULL
                ALTER TABLE [dbo].[Store] ADD [sellerType] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Store_sellerType] DEFAULT('Individual');
            IF COL_LENGTH(N'dbo.Store', N'legalName') IS NULL
                ALTER TABLE [dbo].[Store] ADD [legalName] NVARCHAR(150) NOT NULL CONSTRAINT [DF_Store_legalName] DEFAULT('Legacy seller');
            IF COL_LENGTH(N'dbo.Store', N'contactPhone') IS NULL
                ALTER TABLE [dbo].[Store] ADD [contactPhone] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Store_contactPhone] DEFAULT('Not provided');
            IF COL_LENGTH(N'dbo.Store', N'verificationStatus') IS NULL
                ALTER TABLE [dbo].[Store] ADD [verificationStatus] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Store_verificationStatus] DEFAULT('Pending');
            IF COL_LENGTH(N'dbo.Store', N'rejectionReason') IS NULL
                ALTER TABLE [dbo].[Store] ADD [rejectionReason] NVARCHAR(500) NULL;
            IF COL_LENGTH(N'dbo.Store', N'reviewedByUserId') IS NULL
                ALTER TABLE [dbo].[Store] ADD [reviewedByUserId] INT NULL;
            IF COL_LENGTH(N'dbo.Store', N'reviewedAtUtc') IS NULL
                ALTER TABLE [dbo].[Store] ADD [reviewedAtUtc] DATETIME2 NULL;
            IF COL_LENGTH(N'dbo.Store', N'createdAtUtc') IS NULL
                ALTER TABLE [dbo].[Store] ADD [createdAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Store_createdAtUtc] DEFAULT(SYSUTCDATETIME());
            IF COL_LENGTH(N'dbo.Store', N'updatedAtUtc') IS NULL
                ALTER TABLE [dbo].[Store] ADD [updatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Store_updatedAtUtc] DEFAULT(SYSUTCDATETIME());

            IF OBJECT_ID(N'[dbo].[RefreshToken]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[RefreshToken]
                (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_RefreshToken] PRIMARY KEY,
                    [userId] INT NOT NULL,
                    [tokenHash] NVARCHAR(64) NOT NULL,
                    [expiresAtUtc] DATETIME2 NOT NULL,
                    [createdAtUtc] DATETIME2 NOT NULL,
                    [revokedAtUtc] DATETIME2 NULL,
                    [replacedByTokenHash] NVARCHAR(64) NULL,
                    CONSTRAINT [FK_RefreshToken_User_userId] FOREIGN KEY ([userId]) REFERENCES [dbo].[User]([id]) ON DELETE CASCADE
                );
            END;
            """);

        migrationBuilder.Sql("""
            UPDATE storeRow
            SET [verificationStatus] = 'Approved'
            FROM [dbo].[Store] storeRow
            JOIN [dbo].[User] userRow ON userRow.[id] = storeRow.[sellerId]
            WHERE userRow.[role] = 'Seller';

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshToken_tokenHash' AND object_id = OBJECT_ID(N'[dbo].[RefreshToken]'))
                CREATE UNIQUE INDEX [IX_RefreshToken_tokenHash] ON [dbo].[RefreshToken]([tokenHash]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshToken_userId_expiresAtUtc' AND object_id = OBJECT_ID(N'[dbo].[RefreshToken]'))
                CREATE INDEX [IX_RefreshToken_userId_expiresAtUtc] ON [dbo].[RefreshToken]([userId], [expiresAtUtc]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_sellerId' AND object_id = OBJECT_ID(N'[dbo].[Store]'))
                CREATE UNIQUE INDEX [IX_Store_sellerId] ON [dbo].[Store]([sellerId]) WHERE [sellerId] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_storeName' AND object_id = OBJECT_ID(N'[dbo].[Store]'))
                CREATE UNIQUE INDEX [IX_Store_storeName] ON [dbo].[Store]([storeName]) WHERE [storeName] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_verificationStatus' AND object_id = OBJECT_ID(N'[dbo].[Store]'))
                CREATE INDEX [IX_Store_verificationStatus] ON [dbo].[Store]([verificationStatus]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[RefreshToken]', N'U') IS NOT NULL DROP TABLE [dbo].[RefreshToken];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_verificationStatus' AND object_id = OBJECT_ID(N'[dbo].[Store]')) DROP INDEX [IX_Store_verificationStatus] ON [dbo].[Store];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_storeName' AND object_id = OBJECT_ID(N'[dbo].[Store]')) DROP INDEX [IX_Store_storeName] ON [dbo].[Store];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Store_sellerId' AND object_id = OBJECT_ID(N'[dbo].[Store]')) DROP INDEX [IX_Store_sellerId] ON [dbo].[Store];

            IF COL_LENGTH(N'dbo.Store', N'updatedAtUtc') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_updatedAtUtc], COLUMN [updatedAtUtc];
            IF COL_LENGTH(N'dbo.Store', N'createdAtUtc') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_createdAtUtc], COLUMN [createdAtUtc];
            IF COL_LENGTH(N'dbo.Store', N'reviewedAtUtc') IS NOT NULL ALTER TABLE [dbo].[Store] DROP COLUMN [reviewedAtUtc];
            IF COL_LENGTH(N'dbo.Store', N'reviewedByUserId') IS NOT NULL ALTER TABLE [dbo].[Store] DROP COLUMN [reviewedByUserId];
            IF COL_LENGTH(N'dbo.Store', N'rejectionReason') IS NOT NULL ALTER TABLE [dbo].[Store] DROP COLUMN [rejectionReason];
            IF COL_LENGTH(N'dbo.Store', N'verificationStatus') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_verificationStatus], COLUMN [verificationStatus];
            IF COL_LENGTH(N'dbo.Store', N'contactPhone') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_contactPhone], COLUMN [contactPhone];
            IF COL_LENGTH(N'dbo.Store', N'legalName') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_legalName], COLUMN [legalName];
            IF COL_LENGTH(N'dbo.Store', N'sellerType') IS NOT NULL ALTER TABLE [dbo].[Store] DROP CONSTRAINT [DF_Store_sellerType], COLUMN [sellerType];

            IF COL_LENGTH(N'dbo.User', N'updatedAtUtc') IS NOT NULL ALTER TABLE [dbo].[User] DROP CONSTRAINT [DF_User_updatedAtUtc], COLUMN [updatedAtUtc];
            IF COL_LENGTH(N'dbo.User', N'createdAtUtc') IS NOT NULL ALTER TABLE [dbo].[User] DROP CONSTRAINT [DF_User_createdAtUtc], COLUMN [createdAtUtc];
            IF COL_LENGTH(N'dbo.User', N'lockoutEndUtc') IS NOT NULL ALTER TABLE [dbo].[User] DROP COLUMN [lockoutEndUtc];
            IF COL_LENGTH(N'dbo.User', N'failedLoginAttempts') IS NOT NULL ALTER TABLE [dbo].[User] DROP CONSTRAINT [DF_User_failedLoginAttempts], COLUMN [failedLoginAttempts];
            IF COL_LENGTH(N'dbo.User', N'isActive') IS NOT NULL ALTER TABLE [dbo].[User] DROP CONSTRAINT [DF_User_isActive], COLUMN [isActive];
            IF COL_LENGTH(N'dbo.User', N'emailConfirmed') IS NOT NULL ALTER TABLE [dbo].[User] DROP CONSTRAINT [DF_User_emailConfirmed], COLUMN [emailConfirmed];
            """);
    }
}

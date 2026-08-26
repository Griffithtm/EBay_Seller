using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneEbay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CloneEbayDbContext))]
[Migration("20260824110000_AddRole5CustomerSupport")]
public sealed class AddRole5CustomerSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[User]', N'U') IS NULL
                OR OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL
                OR OBJECT_ID(N'[dbo].[OrderTable]', N'U') IS NULL
                THROW 51005, 'Apply the Role 1 and Role 2 migrations before the Role 5 customer-support migration.', 1;

            IF OBJECT_ID(N'[dbo].[Review]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Review] (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Review] PRIMARY KEY,
                    [productId] INT NOT NULL,
                    [reviewerId] INT NOT NULL,
                    [rating] INT NOT NULL,
                    [comment] NVARCHAR(2000) NOT NULL,
                    [createdAt] DATETIME2 NOT NULL CONSTRAINT [DF_Review_createdAt] DEFAULT(SYSUTCDATETIME()),
                    CONSTRAINT [FK_Review_Product_productId] FOREIGN KEY ([productId]) REFERENCES [dbo].[Product]([id]),
                    CONSTRAINT [FK_Review_User_reviewerId] FOREIGN KEY ([reviewerId]) REFERENCES [dbo].[User]([id]),
                    CONSTRAINT [CK_Review_Rating] CHECK ([rating] BETWEEN 1 AND 5)
                );
                CREATE INDEX [IX_Review_productId] ON [dbo].[Review]([productId]);
                CREATE INDEX [IX_Review_reviewerId] ON [dbo].[Review]([reviewerId]);
                CREATE UNIQUE INDEX [IX_Review_productId_reviewerId] ON [dbo].[Review]([productId], [reviewerId]);
            END;

            IF OBJECT_ID(N'[dbo].[Feedback]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Feedback] (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Feedback] PRIMARY KEY,
                    [sellerId] INT NOT NULL,
                    [averageRating] DECIMAL(3,2) NOT NULL CONSTRAINT [DF_Feedback_averageRating] DEFAULT(0),
                    [totalReviews] INT NOT NULL CONSTRAINT [DF_Feedback_totalReviews] DEFAULT(0),
                    [positiveRate] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_Feedback_positiveRate] DEFAULT(0),
                    CONSTRAINT [FK_Feedback_User_sellerId] FOREIGN KEY ([sellerId]) REFERENCES [dbo].[User]([id])
                );
                CREATE UNIQUE INDEX [IX_Feedback_sellerId] ON [dbo].[Feedback]([sellerId]);
            END;

            IF OBJECT_ID(N'[dbo].[ReturnRequest]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ReturnRequest] (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReturnRequest] PRIMARY KEY,
                    [orderId] INT NOT NULL,
                    [userId] INT NOT NULL,
                    [reason] NVARCHAR(2000) NOT NULL,
                    [status] NVARCHAR(30) NOT NULL,
                    [createdAt] DATETIME2 NOT NULL CONSTRAINT [DF_ReturnRequest_createdAt] DEFAULT(SYSUTCDATETIME()),
                    CONSTRAINT [FK_ReturnRequest_OrderTable_orderId] FOREIGN KEY ([orderId]) REFERENCES [dbo].[OrderTable]([id]),
                    CONSTRAINT [FK_ReturnRequest_User_userId] FOREIGN KEY ([userId]) REFERENCES [dbo].[User]([id])
                );
                CREATE INDEX [IX_ReturnRequest_orderId] ON [dbo].[ReturnRequest]([orderId]);
                CREATE INDEX [IX_ReturnRequest_userId] ON [dbo].[ReturnRequest]([userId]);
                CREATE INDEX [IX_ReturnRequest_status] ON [dbo].[ReturnRequest]([status]);
            END;

            IF OBJECT_ID(N'[dbo].[Dispute]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Dispute] (
                    [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Dispute] PRIMARY KEY,
                    [orderId] INT NOT NULL,
                    [raisedBy] INT NOT NULL,
                    [description] NVARCHAR(2000) NOT NULL,
                    [status] NVARCHAR(20) NOT NULL,
                    [resolution] NVARCHAR(2000) NULL,
                    CONSTRAINT [FK_Dispute_OrderTable_orderId] FOREIGN KEY ([orderId]) REFERENCES [dbo].[OrderTable]([id]),
                    CONSTRAINT [FK_Dispute_User_raisedBy] FOREIGN KEY ([raisedBy]) REFERENCES [dbo].[User]([id])
                );
                CREATE INDEX [IX_Dispute_orderId] ON [dbo].[Dispute]([orderId]);
                CREATE INDEX [IX_Dispute_raisedBy] ON [dbo].[Dispute]([raisedBy]);
                CREATE INDEX [IX_Dispute_status] ON [dbo].[Dispute]([status]);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Dispute]', N'U') IS NOT NULL DROP TABLE [dbo].[Dispute];
            IF OBJECT_ID(N'[dbo].[ReturnRequest]', N'U') IS NOT NULL DROP TABLE [dbo].[ReturnRequest];
            IF OBJECT_ID(N'[dbo].[Feedback]', N'U') IS NOT NULL DROP TABLE [dbo].[Feedback];
            IF OBJECT_ID(N'[dbo].[Review]', N'U') IS NOT NULL DROP TABLE [dbo].[Review];
            """);
    }
}

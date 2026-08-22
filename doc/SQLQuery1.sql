USE [master]
GO
/****** Object:  Database [CloneEbayDB]    Script Date: 8/23/2026 2:12:57 AM ******/
CREATE DATABASE [CloneEbayDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'CloneEbayDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CloneEbayDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'CloneEbayDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CloneEbayDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [CloneEbayDB] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [CloneEbayDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [CloneEbayDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [CloneEbayDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [CloneEbayDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [CloneEbayDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [CloneEbayDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [CloneEbayDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [CloneEbayDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [CloneEbayDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [CloneEbayDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [CloneEbayDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [CloneEbayDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [CloneEbayDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [CloneEbayDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [CloneEbayDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [CloneEbayDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [CloneEbayDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [CloneEbayDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [CloneEbayDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [CloneEbayDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [CloneEbayDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [CloneEbayDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [CloneEbayDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [CloneEbayDB] SET RECOVERY FULL 
GO
ALTER DATABASE [CloneEbayDB] SET  MULTI_USER 
GO
ALTER DATABASE [CloneEbayDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [CloneEbayDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [CloneEbayDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [CloneEbayDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [CloneEbayDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [CloneEbayDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'CloneEbayDB', N'ON'
GO
ALTER DATABASE [CloneEbayDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [CloneEbayDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [CloneEbayDB]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Address]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Address](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NULL,
	[fullName] [nvarchar](100) NULL,
	[phone] [nvarchar](20) NULL,
	[street] [nvarchar](100) NULL,
	[city] [nvarchar](50) NULL,
	[state] [nvarchar](50) NULL,
	[country] [nvarchar](50) NULL,
	[isDefault] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Bid]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bid](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[productId] [int] NULL,
	[bidderId] [int] NULL,
	[amount] [decimal](10, 2) NULL,
	[bidTime] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Category]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Category](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Coupon]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Coupon](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[code] [nvarchar](50) NULL,
	[discountPercent] [decimal](5, 2) NULL,
	[startDate] [datetime] NULL,
	[endDate] [datetime] NULL,
	[maxUsage] [int] NULL,
	[productId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Dispute]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Dispute](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[orderId] [int] NULL,
	[raisedBy] [int] NULL,
	[description] [nvarchar](max) NULL,
	[status] [nvarchar](20) NULL,
	[resolution] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Feedback]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Feedback](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[sellerId] [int] NULL,
	[averageRating] [decimal](3, 2) NULL,
	[totalReviews] [int] NULL,
	[positiveRate] [decimal](5, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Inventory]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Inventory](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[productId] [int] NULL,
	[quantity] [int] NULL,
	[lastUpdated] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Message]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Message](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[senderId] [int] NULL,
	[receiverId] [int] NULL,
	[content] [nvarchar](max) NULL,
	[timestamp] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderItem]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderItem](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[orderId] [int] NULL,
	[productId] [int] NULL,
	[quantity] [int] NULL,
	[unitPrice] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderTable]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderTable](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[buyerId] [int] NULL,
	[addressId] [int] NULL,
	[orderDate] [datetime] NULL,
	[totalPrice] [decimal](10, 2) NULL,
	[status] [nvarchar](20) NULL,
	[auctionProductId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payment]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[orderId] [int] NULL,
	[userId] [int] NULL,
	[amount] [decimal](10, 2) NULL,
	[method] [nvarchar](50) NULL,
	[status] [nvarchar](20) NULL,
	[paidAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[title] [nvarchar](255) NULL,
	[description] [nvarchar](max) NULL,
	[price] [decimal](10, 2) NULL,
	[images] [nvarchar](max) NULL,
	[categoryId] [int] NULL,
	[sellerId] [int] NULL,
	[isAuction] [bit] NULL,
	[auctionEndTime] [datetime] NULL,
	[status] [nvarchar](20) NOT NULL,
	[createdAtUtc] [datetime2](7) NOT NULL,
	[updatedAtUtc] [datetime2](7) NOT NULL,
	[auctionClosedAtUtc] [datetime2](7) NULL,
	[auctionWinnerId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshToken]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RefreshToken](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NOT NULL,
	[tokenHash] [nvarchar](64) NOT NULL,
	[expiresAtUtc] [datetime2](7) NOT NULL,
	[createdAtUtc] [datetime2](7) NOT NULL,
	[revokedAtUtc] [datetime2](7) NULL,
	[replacedByTokenHash] [nvarchar](64) NULL,
 CONSTRAINT [PK_RefreshToken] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ReturnRequest]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReturnRequest](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[orderId] [int] NULL,
	[userId] [int] NULL,
	[reason] [nvarchar](max) NULL,
	[status] [nvarchar](20) NULL,
	[createdAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Review]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Review](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[productId] [int] NULL,
	[reviewerId] [int] NULL,
	[rating] [int] NULL,
	[comment] [nvarchar](max) NULL,
	[createdAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShippingInfo]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShippingInfo](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[orderId] [int] NULL,
	[carrier] [nvarchar](100) NULL,
	[trackingNumber] [nvarchar](100) NULL,
	[status] [nvarchar](50) NULL,
	[estimatedArrival] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Store]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Store](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[sellerId] [int] NULL,
	[storeName] [nvarchar](100) NULL,
	[description] [nvarchar](max) NULL,
	[bannerImageURL] [nvarchar](max) NULL,
	[sellerType] [nvarchar](20) NOT NULL,
	[legalName] [nvarchar](150) NOT NULL,
	[contactPhone] [nvarchar](20) NOT NULL,
	[verificationStatus] [nvarchar](20) NOT NULL,
	[rejectionReason] [nvarchar](500) NULL,
	[reviewedByUserId] [int] NULL,
	[reviewedAtUtc] [datetime2](7) NULL,
	[createdAtUtc] [datetime2](7) NOT NULL,
	[updatedAtUtc] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 8/23/2026 2:12:57 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[username] [nvarchar](100) NULL,
	[email] [nvarchar](100) NULL,
	[password] [nvarchar](255) NULL,
	[role] [nvarchar](20) NULL,
	[avatarURL] [nvarchar](max) NULL,
	[emailConfirmed] [bit] NOT NULL,
	[isActive] [bit] NOT NULL,
	[failedLoginAttempts] [int] NOT NULL,
	[lockoutEndUtc] [datetime2](7) NULL,
	[createdAtUtc] [datetime2](7) NOT NULL,
	[updatedAtUtc] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260822120000_UpgradeRole1Auth', N'8.0.30')
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260822220000_AddRole2Catalog', N'8.0.30')
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260823120000_CompleteRole2OrdersAndConcurrency', N'8.0.30')
GO
SET IDENTITY_INSERT [dbo].[Bid] ON 
GO
INSERT [dbo].[Bid] ([id], [productId], [bidderId], [amount], [bidTime]) VALUES (1, 7, 4, CAST(6900000.00 AS Decimal(10, 2)), CAST(N'2026-08-22T16:26:48.813' AS DateTime))
GO
INSERT [dbo].[Bid] ([id], [productId], [bidderId], [amount], [bidTime]) VALUES (2, 9, 8, CAST(125000.00 AS Decimal(10, 2)), CAST(N'2026-08-22T18:53:16.123' AS DateTime))
GO
SET IDENTITY_INSERT [dbo].[Bid] OFF
GO
SET IDENTITY_INSERT [dbo].[Category] ON 
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (1, N'Điện tử & Công nghệ')
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (2, N'Thời trang')
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (3, N'Nhà cửa & Đời sống')
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (4, N'Thể thao & Dã ngoại')
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (5, N'Sưu tầm & Nghệ thuật')
GO
INSERT [dbo].[Category] ([id], [name]) VALUES (6, N'Phụ tùng & Xe cộ')
GO
SET IDENTITY_INSERT [dbo].[Category] OFF
GO
SET IDENTITY_INSERT [dbo].[Inventory] ON 
GO
INSERT [dbo].[Inventory] ([id], [productId], [quantity], [lastUpdated]) VALUES (1, 1, 18, CAST(N'2026-08-22T13:21:45.967' AS DateTime))
GO
INSERT [dbo].[Inventory] ([id], [productId], [quantity], [lastUpdated]) VALUES (3, 3, 0, CAST(N'2026-08-21T16:21:45.967' AS DateTime))
GO
INSERT [dbo].[Inventory] ([id], [productId], [quantity], [lastUpdated]) VALUES (4, 4, 7, CAST(N'2026-08-22T08:21:45.967' AS DateTime))
GO
INSERT [dbo].[Inventory] ([id], [productId], [quantity], [lastUpdated]) VALUES (7, 7, 1, CAST(N'2026-08-22T15:41:14.927' AS DateTime))
GO
INSERT [dbo].[Inventory] ([id], [productId], [quantity], [lastUpdated]) VALUES (9, 9, 0, CAST(N'2026-08-22T18:54:31.210' AS DateTime))
GO
SET IDENTITY_INSERT [dbo].[Inventory] OFF
GO
SET IDENTITY_INSERT [dbo].[OrderItem] ON 
GO
INSERT [dbo].[OrderItem] ([id], [orderId], [productId], [quantity], [unitPrice]) VALUES (1, 1, 9, 1, CAST(125000.00 AS Decimal(10, 2)))
GO
SET IDENTITY_INSERT [dbo].[OrderItem] OFF
GO
SET IDENTITY_INSERT [dbo].[OrderTable] ON 
GO
INSERT [dbo].[OrderTable] ([id], [buyerId], [addressId], [orderDate], [totalPrice], [status], [auctionProductId]) VALUES (1, 8, NULL, CAST(N'2026-08-22T18:54:31.210' AS DateTime), CAST(125000.00 AS Decimal(10, 2)), N'WaitingPayment', 9)
GO
SET IDENTITY_INSERT [dbo].[OrderTable] OFF
GO
SET IDENTITY_INSERT [dbo].[Product] ON 
GO
INSERT [dbo].[Product] ([id], [title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime], [status], [createdAtUtc], [updatedAtUtc], [auctionClosedAtUtc], [auctionWinnerId]) VALUES (1, N'Studio Pro ANC Headphones', N'Tai nghe chống ồn chủ động, âm thanh cân bằng và thời lượng pin cho cả ngày làm việc.', CAST(3290000.00 AS Decimal(10, 2)), N'[]', 1, 3, 0, NULL, N'Active', CAST(N'2026-08-14T16:21:45.9661963' AS DateTime2), CAST(N'2026-08-22T13:21:45.9661963' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[Product] ([id], [title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime], [status], [createdAtUtc], [updatedAtUtc], [auctionClosedAtUtc], [auctionWinnerId]) VALUES (3, N'Everyday Linen Carry Tote', N'Túi tote linen tối giản, ngăn chứa rộng và phù hợp cho nhịp sống hằng ngày.', CAST(790000.00 AS Decimal(10, 2)), N'[]', 2, 3, 0, NULL, N'OutOfStock', CAST(N'2026-08-10T16:21:45.9661963' AS DateTime2), CAST(N'2026-08-21T16:21:45.9661963' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[Product] ([id], [title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime], [status], [createdAtUtc], [updatedAtUtc], [auctionClosedAtUtc], [auctionWinnerId]) VALUES (4, N'Nimbus 75 Mechanical Keyboard', N'Bàn phím cơ layout 75%, hot-swap, kết nối đa thiết bị và đèn nền tinh tế.', CAST(2490000.00 AS Decimal(10, 2)), N'[]', 1, 3, 0, NULL, N'Hidden', CAST(N'2026-08-17T16:21:45.9661963' AS DateTime2), CAST(N'2026-08-22T08:21:45.9661963' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[Product] ([id], [title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime], [status], [createdAtUtc], [updatedAtUtc], [auctionClosedAtUtc], [auctionWinnerId]) VALUES (7, N'Heritage Automatic Chronograph', N'Đồng hồ cơ phiên bản sưu tầm, mặt số cổ điển và dây da hoàn thiện thủ công.', CAST(6800000.00 AS Decimal(10, 2)), N'[]', 5, 3, 1, CAST(N'2026-08-24T16:26:14.927' AS DateTime), N'Active', CAST(N'2026-08-20T16:26:14.9278481' AS DateTime2), CAST(N'2026-08-22T16:26:48.8138976' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[Product] ([id], [title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime], [status], [createdAtUtc], [updatedAtUtc], [auctionClosedAtUtc], [auctionWinnerId]) VALUES (9, N'Auction Smoke 576207233c', N'Automated auction close and order idempotency acceptance test.', CAST(100000.00 AS Decimal(10, 2)), N'["/uploads/products/4ef62ce8859145e08787d3c46c878c5a.png"]', 1, 3, 1, CAST(N'2026-08-22T18:54:25.870' AS DateTime), N'AuctionEnded', CAST(N'2026-08-22T18:53:15.9515938' AS DateTime2), CAST(N'2026-08-22T18:54:31.2083349' AS DateTime2), CAST(N'2026-08-22T18:54:31.2083349' AS DateTime2), 8)
GO
SET IDENTITY_INSERT [dbo].[Product] OFF
GO
SET IDENTITY_INSERT [dbo].[RefreshToken] ON 
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (1, 2, N'13385DB53F174310442C226E90C9FFFADCCE0E5487C7004D2094E8F233992BBF', CAST(N'2026-08-29T11:29:32.7345212' AS DateTime2), CAST(N'2026-08-22T11:29:32.7347280' AS DateTime2), CAST(N'2026-08-22T11:29:33.1258596' AS DateTime2), N'70CD5C39B15EF718055F24B23D152864EDF9AC4A34F13786B169AC82CE72D77A')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (2, 1, N'EAD43C8FDFC4432624C8FB6B39A5BCFFC04BF9B8BF05EEE7C1496871CD6E4D2B', CAST(N'2026-08-29T11:29:32.9774826' AS DateTime2), CAST(N'2026-08-22T11:29:32.9774852' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (3, 2, N'70CD5C39B15EF718055F24B23D152864EDF9AC4A34F13786B169AC82CE72D77A', CAST(N'2026-08-29T11:29:33.1260716' AS DateTime2), CAST(N'2026-08-22T11:29:33.1258596' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (4, 6, N'D065423B20E73C7993B8BFAAFC0072F74D2FD4985D6999977EA8636B316ADC7B', CAST(N'2026-08-29T14:19:50.4172033' AS DateTime2), CAST(N'2026-08-22T14:19:50.4173400' AS DateTime2), CAST(N'2026-08-22T14:19:50.6643954' AS DateTime2), N'CC253A9C7FC60F9E038D571E952DE77906172D6EC6D5D15DEBBB3E5BE35B286D')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (5, 1, N'CB2D161740411E2CCD2A8706FD01CC5EA02C2C0CC02B2019D4E439E072BA6796', CAST(N'2026-08-29T14:19:50.5977599' AS DateTime2), CAST(N'2026-08-22T14:19:50.5977616' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (6, 6, N'CC253A9C7FC60F9E038D571E952DE77906172D6EC6D5D15DEBBB3E5BE35B286D', CAST(N'2026-08-29T14:19:50.6646269' AS DateTime2), CAST(N'2026-08-22T14:19:50.6643954' AS DateTime2), CAST(N'2026-08-22T14:19:50.7280888' AS DateTime2), NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (7, 3, N'C21AFCACFE543EDBFD74A5BAD216B22397C669C872090C8020BCD5C55B1D4E3D', CAST(N'2026-08-29T14:20:24.4462172' AS DateTime2), CAST(N'2026-08-22T14:20:24.4462206' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (8, 4, N'D0E4A1F6B768328E849DA111248CC7D43FDEC95C5F297B1815A9F3BF8B69D9CB', CAST(N'2026-08-29T14:20:24.5079726' AS DateTime2), CAST(N'2026-08-22T14:20:24.5079748' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (9, 5, N'7FD463DD368AEBE7B9D583676AA6D6850A8AA2836E7309EC15E48B552BDA8B09', CAST(N'2026-08-29T14:20:24.5364882' AS DateTime2), CAST(N'2026-08-22T14:20:24.5364899' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (10, 7, N'6151CE51BDAB67195CE3DAD382F5FEDD22E0E03BFE3FCFC8E898981F8251D0CD', CAST(N'2026-08-29T14:20:24.6517439' AS DateTime2), CAST(N'2026-08-22T14:20:24.6517464' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (11, 3, N'D189D59589F6D040D9D8E86BC382123E20EF22AC7A4D0BCACFB8CC1354D3C0AF', CAST(N'2026-08-29T14:21:13.5059243' AS DateTime2), CAST(N'2026-08-22T14:21:13.5059286' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (12, 3, N'32CC749CB8787FC5424C4C7C59A06198607F5377ED645A5F70A9A55F6AF70A88', CAST(N'2026-08-29T14:24:36.6892149' AS DateTime2), CAST(N'2026-08-22T14:24:36.6894724' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (13, 3, N'C33940D8A597E5BD9CEC0A6BC374C93D9A630340D76BB7BE6F7401859389D632', CAST(N'2026-08-29T15:12:28.2393627' AS DateTime2), CAST(N'2026-08-22T15:12:28.2396954' AS DateTime2), CAST(N'2026-08-22T15:52:10.6535485' AS DateTime2), N'A70564E64E241C120AA1ECD73936DE94964D676193ADF8C953C72F24725BC347')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (14, 3, N'D7883A50C6617A173557E26BAA29FB6F799744345630FFB43F69CCE2F0C81CF9', CAST(N'2026-08-29T15:45:51.5787013' AS DateTime2), CAST(N'2026-08-22T15:45:51.5790603' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (15, 1, N'1C357CAF0B0BDFD7213E01E6407B6B80DD4686ADF39948F79BEFF5976FF8D309', CAST(N'2026-08-29T15:45:52.0543601' AS DateTime2), CAST(N'2026-08-22T15:45:52.0543625' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (16, 3, N'7FA8866DAFC0987123526CDA2CFE233D2EFEF6E5AF6A46552333FB63416574AB', CAST(N'2026-08-29T15:46:05.4521372' AS DateTime2), CAST(N'2026-08-22T15:46:05.4521394' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (17, 3, N'B332152BF51EC5FE65639C3BAA7103D380507D8AF8213BCF14BE791B29D204B2', CAST(N'2026-08-29T15:49:45.4937365' AS DateTime2), CAST(N'2026-08-22T15:49:45.4940289' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (18, 1, N'4441F0090CF2FCE6BDA80B89103EDCC621685CE752B0AC0E2E5528AB3CAE5F8C', CAST(N'2026-08-29T15:49:45.9024203' AS DateTime2), CAST(N'2026-08-22T15:49:45.9024238' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (19, 3, N'A70564E64E241C120AA1ECD73936DE94964D676193ADF8C953C72F24725BC347', CAST(N'2026-08-29T15:52:10.6557635' AS DateTime2), CAST(N'2026-08-22T15:52:10.6535485' AS DateTime2), CAST(N'2026-08-22T17:09:55.3613202' AS DateTime2), N'94E29695F7D74EAD5167B5FCB1B90FD0208CDA5A35ABA12467A58B73D513FB01')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (20, 3, N'33E4A4B803E499AF46D398538F1B9231DB285F5089624A3AB46D70DBB52B2A92', CAST(N'2026-08-29T16:22:32.2991492' AS DateTime2), CAST(N'2026-08-22T16:22:32.2993100' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (21, 3, N'572C8B979045E5D0979FD8BF7CECCE72C2551493DFDA2D657E05AB6F59802398', CAST(N'2026-08-29T16:23:30.2572693' AS DateTime2), CAST(N'2026-08-22T16:23:30.2572742' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (22, 3, N'18BBC802407DEF04B0BF75348FA45B38B1669C4E970D893C8D610DA6402B1219', CAST(N'2026-08-29T16:24:57.6675694' AS DateTime2), CAST(N'2026-08-22T16:24:57.6677131' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (23, 3, N'1A338C226191671AA2BF3E1145A2FA6BB4E672578158F9213AF0B789D4D912F5', CAST(N'2026-08-29T16:25:24.7771276' AS DateTime2), CAST(N'2026-08-22T16:25:24.7771295' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (24, 4, N'E5035EB9074749154AC595C6ED87E7185775C9672559AAC1C22C2FF1922526A1', CAST(N'2026-08-29T16:25:24.8276817' AS DateTime2), CAST(N'2026-08-22T16:25:24.8276839' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (25, 3, N'4A0CC119EB2B7186F74BDCAFA4D61929B38828F49D9631D33184B540DA024758', CAST(N'2026-08-29T16:26:48.4486941' AS DateTime2), CAST(N'2026-08-22T16:26:48.4488792' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (26, 4, N'490139DCA72393C2EE0A0B230FF4F42A6CD33AC9417FED35F15833E482B8DDFC', CAST(N'2026-08-29T16:26:48.5664551' AS DateTime2), CAST(N'2026-08-22T16:26:48.5664600' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (27, 3, N'B452A93A19E204459B18571B6EDDDD26A8C9B45A9398166FA79BB4FCCAF81BF8', CAST(N'2026-08-29T16:28:44.8330734' AS DateTime2), CAST(N'2026-08-22T16:28:44.8330754' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (28, 3, N'29381593247C1794486E17265DD1851A8D710D32E30DE8B3BC1AD1CE8F880B86', CAST(N'2026-08-29T16:30:14.4461630' AS DateTime2), CAST(N'2026-08-22T16:30:14.4461651' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (29, 4, N'32A82B926D9C2B6420DA78456AAC41F641C4E77F2C1B0DD168B324243C988727', CAST(N'2026-08-29T16:30:14.4893508' AS DateTime2), CAST(N'2026-08-22T16:30:14.4893550' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (30, 3, N'94E29695F7D74EAD5167B5FCB1B90FD0208CDA5A35ABA12467A58B73D513FB01', CAST(N'2026-08-29T17:09:55.3642716' AS DateTime2), CAST(N'2026-08-22T17:09:55.3613202' AS DateTime2), CAST(N'2026-08-22T19:01:50.7152061' AS DateTime2), N'F672C9CC066ED9570F253A84D91F800384300417B217B903C7FFEA222E2B6459')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (31, 3, N'B7E778719DBCC1E18802F01E6A41DC9BB6BBD3E5362A1D59CCCE7F55C6ABA46B', CAST(N'2026-08-29T18:52:13.4008999' AS DateTime2), CAST(N'2026-08-22T18:52:13.4013021' AS DateTime2), CAST(N'2026-08-22T18:52:14.7599451' AS DateTime2), NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (32, 3, N'759D227A6D7C7E179FF865D24366C099E1F3C6DEA34BAEDCA96BAFEA822E2C46', CAST(N'2026-08-29T18:53:15.7998056' AS DateTime2), CAST(N'2026-08-22T18:53:15.7998081' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (33, 8, N'D1B198CD1D2E6B554A0D82908AFC6403E2070B3E830F19E518B2BE8F1E1C7B4B', CAST(N'2026-08-29T18:53:16.0750681' AS DateTime2), CAST(N'2026-08-22T18:53:16.0750712' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (34, 9, N'4139A82DFDF1B2E4A727D96D111427A03A8A00E79D228040A9E35DEE664EB8A3', CAST(N'2026-08-29T18:57:52.1985293' AS DateTime2), CAST(N'2026-08-22T18:57:52.1986619' AS DateTime2), CAST(N'2026-08-22T18:57:52.4923122' AS DateTime2), N'2CADE2424D910C7D0180180BF9D796C311A5B1EFB8EA3C4646599B3E4257C26C')
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (35, 1, N'730A780519A3B77AA620DC584821F8BCD4E8E2D8D752F1F0492253C599E0B1D4', CAST(N'2026-08-29T18:57:52.4269106' AS DateTime2), CAST(N'2026-08-22T18:57:52.4269144' AS DateTime2), NULL, NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (36, 9, N'2CADE2424D910C7D0180180BF9D796C311A5B1EFB8EA3C4646599B3E4257C26C', CAST(N'2026-08-29T18:57:52.4924373' AS DateTime2), CAST(N'2026-08-22T18:57:52.4923122' AS DateTime2), CAST(N'2026-08-22T18:57:52.5587388' AS DateTime2), NULL)
GO
INSERT [dbo].[RefreshToken] ([id], [userId], [tokenHash], [expiresAtUtc], [createdAtUtc], [revokedAtUtc], [replacedByTokenHash]) VALUES (37, 3, N'F672C9CC066ED9570F253A84D91F800384300417B217B903C7FFEA222E2B6459', CAST(N'2026-08-29T19:01:50.7181602' AS DateTime2), CAST(N'2026-08-22T19:01:50.7152061' AS DateTime2), NULL, NULL)
GO
SET IDENTITY_INSERT [dbo].[RefreshToken] OFF
GO
SET IDENTITY_INSERT [dbo].[Store] ON 
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (1, 2, N'Role 1 Demo Store', N'Verified demo store created by the Role 1 end-to-end smoke test.', NULL, N'Individual', N'Demo Seller', N'0901234567', N'Approved', NULL, 1, CAST(N'2026-08-22T11:29:33.0845909' AS DateTime2), CAST(N'2026-08-22T11:29:32.8920085' AS DateTime2), CAST(N'2026-08-22T11:29:33.1795907' AS DateTime2))
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (2, 3, N'Role 1 Approved Store', N'Multipart partial update verified at 2026-08-22', NULL, N'Individual', N'seller-approved demo', N'0900000000', N'Approved', NULL, 1, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:20:24.5862249' AS DateTime2))
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (3, 4, N'Role 1 Pending Store', N'Development demo store in Pending state.', NULL, N'Individual', N'seller-pending demo', N'0900000000', N'Pending', NULL, NULL, NULL, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2))
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (4, 5, N'Role 1 Rejected Store', N'Development demo store in Rejected state.', NULL, N'Individual', N'seller-rejected demo', N'0900000000', N'Rejected', N'Demo rejection: please update the contact information.', 1, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2))
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (5, 6, N'Role 1 Smoke 095ebb0d9e', N'Role 1 partial-update smoke test 095ebb0d9e', NULL, N'Individual', N'Role One Smoke Tester', N'0901234567', N'Approved', NULL, 1, CAST(N'2026-08-22T14:19:50.6290597' AS DateTime2), CAST(N'2026-08-22T14:19:50.5406292' AS DateTime2), CAST(N'2026-08-22T14:19:50.6941652' AS DateTime2))
GO
INSERT [dbo].[Store] ([id], [sellerId], [storeName], [description], [bannerImageURL], [sellerType], [legalName], [contactPhone], [verificationStatus], [rejectionReason], [reviewedByUserId], [reviewedAtUtc], [createdAtUtc], [updatedAtUtc]) VALUES (6, 9, N'Role 1 Smoke 7da8ac764e', N'Role 1 partial-update smoke test 7da8ac764e', NULL, N'Individual', N'Role One Smoke Tester', N'0901234567', N'Approved', NULL, 1, CAST(N'2026-08-22T18:57:52.4548400' AS DateTime2), CAST(N'2026-08-22T18:57:52.3682037' AS DateTime2), CAST(N'2026-08-22T18:57:52.5239158' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[Store] OFF
GO
SET IDENTITY_INSERT [dbo].[User] ON 
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (1, N'Administrator', N'admin@cloneebay.local', N'PBKDF2$120000$ERcIfni5zwRH/3TQJ3LFeg==$FfDA3xhSgHsxfJ8itZyvjxob/u8OyOJtDeUke3MvCIM=', N'Admin', NULL, 1, 1, 0, NULL, CAST(N'2026-08-22T11:28:52.1289510' AS DateTime2), CAST(N'2026-08-22T19:01:33.3022887' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (2, N'Demo Seller', N'seller.demo@cloneebay.local', N'PBKDF2$120000$5F5gGfhFaY84q2SN3hz5EA==$jxhiNiajBzwSx4NTVBKBMZn7ZBvau9BWKmFl5CzjcGc=', N'Seller', NULL, 0, 1, 0, NULL, CAST(N'2026-08-22T11:29:32.5825128' AS DateTime2), CAST(N'2026-08-22T11:29:33.0845909' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (3, N'seller-approved', N'seller.approved@cloneebay.local', N'PBKDF2$120000$f45wdM3MoSN4atBDAgG1mQ==$uRz4FVdLOa/A/PPUTwxiPcbYeJKxDXxLUMuTAaxRmvg=', N'Seller', NULL, 1, 1, 0, NULL, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T18:53:15.7806254' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (4, N'seller-pending', N'seller.pending@cloneebay.local', N'PBKDF2$120000$4ZxfF8qZFEyG0jvp1jZfnw==$CBIXWADFUd3qphCIPIWSgm9Nj+qSN7eB0Rvq8/oJJEI=', N'Buyer', NULL, 1, 1, 0, NULL, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T16:30:14.4738897' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (5, N'seller-rejected', N'seller.rejected@cloneebay.local', N'PBKDF2$120000$OS9/6D/UEz5DU/bwpvmQrQ==$XugnbLJiEFsJ2Rg78R5dDvjH6pmRvKKyJWs/VJVeHQU=', N'Buyer', NULL, 1, 1, 0, NULL, CAST(N'2026-08-22T14:19:35.0679071' AS DateTime2), CAST(N'2026-08-22T14:20:24.5208630' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (6, N'role1-095ebb0d9e', N'role1-smoke-095ebb0d9e@cloneebay.local', N'PBKDF2$120000$g5tvK+YE1ewN1o89D4CyFg==$6xfsOl4F66VFNTtUZvAbqvRQ/FQ481tANHSWpS4r5ng=', N'Seller', NULL, 0, 1, 0, NULL, CAST(N'2026-08-22T14:19:50.2689246' AS DateTime2), CAST(N'2026-08-22T14:19:50.6290597' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (7, N'negative-3b4570dee1', N'role1-negative-3b4570dee1@cloneebay.local', N'PBKDF2$120000$QF+eU2yP01mI2VV0AhSH/A==$kZ2GrnyKNa7e4OvP6L7txjDV5hnXoGqPyldnAzThct8=', N'Buyer', NULL, 0, 1, 0, NULL, CAST(N'2026-08-22T14:20:24.6072628' AS DateTime2), CAST(N'2026-08-22T14:20:24.6363913' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (8, N'bidder-576207233c', N'auction-bidder-576207233c@cloneebay.local', N'PBKDF2$120000$6TypMRMNqh9mgJeCAVhfBg==$ew4RNFuPeWxZR1XXRayPowN6h1yFIVs97sronn+sbzM=', N'Buyer', NULL, 0, 1, 0, NULL, CAST(N'2026-08-22T18:53:16.0109333' AS DateTime2), CAST(N'2026-08-22T18:53:16.0546045' AS DateTime2))
GO
INSERT [dbo].[User] ([id], [username], [email], [password], [role], [avatarURL], [emailConfirmed], [isActive], [failedLoginAttempts], [lockoutEndUtc], [createdAtUtc], [updatedAtUtc]) VALUES (9, N'role1-7da8ac764e', N'role1-smoke-7da8ac764e@cloneebay.local', N'PBKDF2$120000$P+j54H3jTbY7M9BxFgST9A==$YGWkASecv5/C4FXC/su9AsWnqJJOWrwvsf8yWwzUu3o=', N'Seller', NULL, 0, 1, 0, NULL, CAST(N'2026-08-22T18:57:52.0384096' AS DateTime2), CAST(N'2026-08-22T18:57:52.4548400' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[User] OFF
GO
/****** Object:  Index [IX_Bid_productId_amount]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_Bid_productId_amount] ON [dbo].[Bid]
(
	[productId] ASC,
	[amount] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Category_name]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Category_name] ON [dbo].[Category]
(
	[name] ASC
)
WHERE ([name] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Inventory_productId]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Inventory_productId] ON [dbo].[Inventory]
(
	[productId] ASC
)
WHERE ([productId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderItem_productId]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderItem_productId] ON [dbo].[OrderItem]
(
	[productId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_OrderTable_auctionProductId]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_OrderTable_auctionProductId] ON [dbo].[OrderTable]
(
	[auctionProductId] ASC
)
WHERE ([auctionProductId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_auctionEndTime]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_Product_auctionEndTime] ON [dbo].[Product]
(
	[auctionEndTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Product_categoryId_status]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_Product_categoryId_status] ON [dbo].[Product]
(
	[categoryId] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Product_sellerId_status]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_Product_sellerId_status] ON [dbo].[Product]
(
	[sellerId] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_RefreshToken_tokenHash]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_RefreshToken_tokenHash] ON [dbo].[RefreshToken]
(
	[tokenHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshToken_userId_expiresAtUtc]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshToken_userId_expiresAtUtc] ON [dbo].[RefreshToken]
(
	[userId] ASC,
	[expiresAtUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Store_sellerId]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Store_sellerId] ON [dbo].[Store]
(
	[sellerId] ASC
)
WHERE ([sellerId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Store_storeName]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Store_storeName] ON [dbo].[Store]
(
	[storeName] ASC
)
WHERE ([storeName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Store_verificationStatus]    Script Date: 8/23/2026 2:12:58 AM ******/
CREATE NONCLUSTERED INDEX [IX_Store_verificationStatus] ON [dbo].[Store]
(
	[verificationStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__User__AB6E6164DD284478]    Script Date: 8/23/2026 2:12:58 AM ******/
ALTER TABLE [dbo].[User] ADD UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Product] ADD  CONSTRAINT [DF_Product_status]  DEFAULT ('Active') FOR [status]
GO
ALTER TABLE [dbo].[Product] ADD  CONSTRAINT [DF_Product_createdAtUtc]  DEFAULT (sysutcdatetime()) FOR [createdAtUtc]
GO
ALTER TABLE [dbo].[Product] ADD  CONSTRAINT [DF_Product_updatedAtUtc]  DEFAULT (sysutcdatetime()) FOR [updatedAtUtc]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_sellerType]  DEFAULT ('Individual') FOR [sellerType]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_legalName]  DEFAULT ('Legacy seller') FOR [legalName]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_contactPhone]  DEFAULT ('Not provided') FOR [contactPhone]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_verificationStatus]  DEFAULT ('Pending') FOR [verificationStatus]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_createdAtUtc]  DEFAULT (sysutcdatetime()) FOR [createdAtUtc]
GO
ALTER TABLE [dbo].[Store] ADD  CONSTRAINT [DF_Store_updatedAtUtc]  DEFAULT (sysutcdatetime()) FOR [updatedAtUtc]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_emailConfirmed]  DEFAULT ((0)) FOR [emailConfirmed]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_isActive]  DEFAULT ((1)) FOR [isActive]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_failedLoginAttempts]  DEFAULT ((0)) FOR [failedLoginAttempts]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_createdAtUtc]  DEFAULT (sysutcdatetime()) FOR [createdAtUtc]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_updatedAtUtc]  DEFAULT (sysutcdatetime()) FOR [updatedAtUtc]
GO
ALTER TABLE [dbo].[Address]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Bid]  WITH CHECK ADD FOREIGN KEY([bidderId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Bid]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Coupon]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Dispute]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[OrderTable] ([id])
GO
ALTER TABLE [dbo].[Dispute]  WITH CHECK ADD FOREIGN KEY([raisedBy])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Feedback]  WITH CHECK ADD FOREIGN KEY([sellerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Inventory]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Message]  WITH CHECK ADD FOREIGN KEY([receiverId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Message]  WITH CHECK ADD FOREIGN KEY([senderId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[OrderTable] ([id])
GO
ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[OrderTable]  WITH CHECK ADD FOREIGN KEY([addressId])
REFERENCES [dbo].[Address] ([id])
GO
ALTER TABLE [dbo].[OrderTable]  WITH CHECK ADD FOREIGN KEY([buyerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[OrderTable]  WITH CHECK ADD  CONSTRAINT [FK_OrderTable_Product_auctionProductId] FOREIGN KEY([auctionProductId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[OrderTable] CHECK CONSTRAINT [FK_OrderTable_Product_auctionProductId]
GO
ALTER TABLE [dbo].[Payment]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[OrderTable] ([id])
GO
ALTER TABLE [dbo].[Payment]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD FOREIGN KEY([categoryId])
REFERENCES [dbo].[Category] ([id])
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD FOREIGN KEY([sellerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_User_auctionWinnerId] FOREIGN KEY([auctionWinnerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_User_auctionWinnerId]
GO
ALTER TABLE [dbo].[RefreshToken]  WITH CHECK ADD  CONSTRAINT [FK_RefreshToken_User_userId] FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RefreshToken] CHECK CONSTRAINT [FK_RefreshToken_User_userId]
GO
ALTER TABLE [dbo].[ReturnRequest]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[OrderTable] ([id])
GO
ALTER TABLE [dbo].[ReturnRequest]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Review]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Review]  WITH CHECK ADD FOREIGN KEY([reviewerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[ShippingInfo]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[OrderTable] ([id])
GO
ALTER TABLE [dbo].[Store]  WITH CHECK ADD FOREIGN KEY([sellerId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Bid]  WITH CHECK ADD  CONSTRAINT [CK_Bid_Amount_Positive] CHECK  (([amount]>(0)))
GO
ALTER TABLE [dbo].[Bid] CHECK CONSTRAINT [CK_Bid_Amount_Positive]
GO
ALTER TABLE [dbo].[Inventory]  WITH CHECK ADD  CONSTRAINT [CK_Inventory_Quantity_NonNegative] CHECK  (([quantity]>=(0)))
GO
ALTER TABLE [dbo].[Inventory] CHECK CONSTRAINT [CK_Inventory_Quantity_NonNegative]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [CK_Product_AuctionEnd] CHECK  (([isAuction]=(0) OR [auctionEndTime] IS NOT NULL))
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [CK_Product_AuctionEnd]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [CK_Product_Price_Positive] CHECK  (([price]>(0)))
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [CK_Product_Price_Positive]
GO
USE [master]
GO
ALTER DATABASE [CloneEbayDB] SET  READ_WRITE 
GO

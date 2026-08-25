using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloneEbay.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        bool seedData = true)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();
        await dbContext.Database.MigrateAsync();
        if (!seedData) return;
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = DateTime.UtcNow;

        var adminEmail = string.IsNullOrWhiteSpace(configuration["Seed:AdminEmail"]) ? "admin@cloneebay.local" : configuration["Seed:AdminEmail"]!.Trim().ToLowerInvariant();
        var adminPassword = string.IsNullOrWhiteSpace(configuration["Seed:AdminPassword"]) ? "Admin123!" : configuration["Seed:AdminPassword"]!;
        var admin = await SeedAdminAsync(dbContext, passwordHasher, configuration, adminEmail, adminPassword, now);

        var demoPassword = string.IsNullOrWhiteSpace(configuration["Seed:DemoSellerPassword"]) ? "Seller123!" : configuration["Seed:DemoSellerPassword"]!;
        await SeedDemoSellersAsync(dbContext, passwordHasher, configuration, demoPassword, admin.Id, now);

        if (IsTrue(configuration, "Seed:DemoProductsEnabled"))
        {
            await SeedDemoProductsAsync(dbContext, now);
        }

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'[dbo].[ReturnRequest]', N'U') IS NOT NULL ALTER TABLE [dbo].[ReturnRequest] ALTER COLUMN [status] NVARCHAR(50) NOT NULL;");
        }
        catch { }

        await SeedCustomPurchaseId5FromId4Async(dbContext, now);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<User> SeedAdminAsync(
        CloneEbayDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        string adminEmail,
        string adminPassword,
        DateTime now)
    {
        var existingAdmin = await dbContext.Users.SingleOrDefaultAsync(user => user.Email == adminEmail);
        if (existingAdmin is not null)
        {
            existingAdmin.PasswordHash = passwordHasher.Hash(adminPassword);
            existingAdmin.Role = UserRole.Admin;
            existingAdmin.IsActive = true;
            existingAdmin.UpdatedAtUtc = now;
            return existingAdmin;
        }

        var admin = new User
        {
            Username = configuration["Seed:AdminUsername"]?.Trim() ?? "Administrator",
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            Role = UserRole.Admin,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
        return admin;
    }

    private static async Task SeedDemoSellersAsync(
        CloneEbayDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        string demoPassword,
        int? adminId,
        DateTime now)
    {
        var seeds = new[]
        {
            new DemoSellerSeed(
                "seller.approved@cloneebay.local",
                "seller-approved",
                "Role 1 Approved Store",
                VerificationStatus.Approved,
                UserRole.Seller,
                null),
            new DemoSellerSeed(
                "seller.pending@cloneebay.local",
                "seller-pending",
                "Role 1 Pending Store",
                VerificationStatus.Pending,
                UserRole.Buyer,
                null),
            new DemoSellerSeed(
                "seller.rejected@cloneebay.local",
                "seller-rejected",
                "Role 1 Rejected Store",
                VerificationStatus.Rejected,
                UserRole.Buyer,
                "Demo rejection: please update the contact information.")
        };

        foreach (var seed in seeds)
        {
            var user = await dbContext.Users
                .Include(candidate => candidate.Store)
                .SingleOrDefaultAsync(candidate => candidate.Email == seed.Email);
            if (user is null)
            {
                user = new User
                {
                    Username = seed.Username,
                    Email = seed.Email,
                    PasswordHash = passwordHasher.Hash(demoPassword),
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = now
                };
                dbContext.Users.Add(user);
            }

            user.PasswordHash = passwordHasher.Hash(demoPassword);
            user.Role = seed.Role;
            user.IsActive = true;
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            user.UpdatedAtUtc = now;

            var store = user.Store;
            if (store is null)
            {
                store = new Store
                {
                    Seller = user,
                    CreatedAtUtc = now
                };
                user.Store = store;
                dbContext.Stores.Add(store);
            }

            store.StoreName = seed.StoreName;
            store.Description = $"Development demo store in {seed.Status} state.";
            store.SellerType = SellerType.Individual;
            store.LegalName = $"{seed.Username} demo";
            store.ContactPhone = "0900000000";
            store.VerificationStatus = seed.Status;
            store.RejectionReason = seed.RejectionReason;
            store.ReviewedByUserId = seed.Status == VerificationStatus.Pending ? null : adminId;
            store.ReviewedAtUtc = seed.Status == VerificationStatus.Pending ? null : now;
            store.UpdatedAtUtc = now;
        }
    }

    private sealed record DemoSellerSeed(
        string Email,
        string Username,
        string StoreName,
        VerificationStatus Status,
        UserRole Role,
        string? RejectionReason);

    private static async Task SeedDemoProductsAsync(CloneEbayDbContext dbContext, DateTime now)
    {
        var seller = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == "seller.approved@cloneebay.local");
        if (seller is null) return;

        var categories = await dbContext.Categories.OrderBy(category => category.Id).ToListAsync();
        if (categories.Count == 0) return;

        Category CategoryAt(int index) => categories[Math.Min(index, categories.Count - 1)];
        var products = new[]
        {
            new Product
            {
                SellerId = seller.Id,
                CategoryId = CategoryAt(0).Id,
                Title = "Studio Pro ANC Headphones",
                Description = "Tai nghe chống ồn chủ động, âm thanh cân bằng và thời lượng pin cho cả ngày làm việc.",
                Price = 3290000m,
                ImagesJson = "[]",
                Status = ProductStatus.Active,
                CreatedAtUtc = now.AddDays(-8),
                UpdatedAtUtc = now.AddHours(-3),
                Inventory = new Inventory { Quantity = 18, LastUpdatedUtc = now.AddHours(-3) }
            },
            new Product
            {
                SellerId = seller.Id,
                CategoryId = CategoryAt(4).Id,
                Title = "Heritage Automatic Chronograph",
                Description = "Đồng hồ cơ phiên bản sưu tầm, mặt số cổ điển và dây da hoàn thiện thủ công.",
                Price = 6800000m,
                ImagesJson = "[]",
                Status = ProductStatus.Active,
                IsAuction = true,
                AuctionEndTimeUtc = now.AddDays(2),
                CreatedAtUtc = now.AddDays(-2),
                UpdatedAtUtc = now.AddMinutes(-45),
                Inventory = new Inventory { Quantity = 1, LastUpdatedUtc = now.AddMinutes(-45) }
            },
            new Product
            {
                SellerId = seller.Id,
                CategoryId = CategoryAt(1).Id,
                Title = "Everyday Linen Carry Tote",
                Description = "Túi tote linen tối giản, ngăn chứa rộng và phù hợp cho nhịp sống hằng ngày.",
                Price = 790000m,
                ImagesJson = "[]",
                Status = ProductStatus.OutOfStock,
                CreatedAtUtc = now.AddDays(-12),
                UpdatedAtUtc = now.AddDays(-1),
                Inventory = new Inventory { Quantity = 0, LastUpdatedUtc = now.AddDays(-1) }
            },
            new Product
            {
                SellerId = seller.Id,
                CategoryId = CategoryAt(0).Id,
                Title = "Nimbus 75 Mechanical Keyboard",
                Description = "Bàn phím cơ layout 75%, hot-swap, kết nối đa thiết bị và đèn nền tinh tế.",
                Price = 2490000m,
                ImagesJson = "[]",
                Status = ProductStatus.Hidden,
                CreatedAtUtc = now.AddDays(-5),
                UpdatedAtUtc = now.AddHours(-8),
                Inventory = new Inventory { Quantity = 7, LastUpdatedUtc = now.AddHours(-8) }
            }
        };
        var existingTitleList = await dbContext.Products
            .Where(product => product.SellerId == seller.Id)
            .Select(product => product.Title)
            .ToListAsync();
        var existingTitles = existingTitleList.ToHashSet(StringComparer.OrdinalIgnoreCase);
        await dbContext.SaveChangesAsync();

        await SeedDemoOrdersAsync(dbContext, seller, now);
    }

    private static async Task SeedDemoOrdersAsync(CloneEbayDbContext dbContext, User seller, DateTime now)
    {
        if (await dbContext.Orders.AnyAsync(o => o.Payment != null && o.Items.Any(i => i.Product.SellerId == seller.Id)))
        {
            return;
        }

        var buyer = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "seller.pending@cloneebay.local") ?? seller;
        var address = new Address
        {
            UserId = buyer.Id,
            FullName = "Demo Buyer",
            Phone = "0987654321",
            Street = "123 Main Street",
            City = "Ho Chi Minh City",
            State = "District 1",
            Country = "Vietnam",
            IsDefault = true
        };
        dbContext.Addresses.Add(address);
        await dbContext.SaveChangesAsync();

        var headphone = await dbContext.Products.FirstOrDefaultAsync(p => p.SellerId == seller.Id && p.Title.Contains("Headphones"));
        var keyboard = await dbContext.Products.FirstOrDefaultAsync(p => p.SellerId == seller.Id && p.Title.Contains("Keyboard"));

        if (headphone is not null)
        {
            var order1 = new Order
            {
                BuyerId = buyer.Id,
                AddressId = address.Id,
                OrderDateUtc = now.AddHours(-1),
                TotalPrice = headphone.Price,
                Status = "Pending",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = headphone.Id, Quantity = 1, UnitPrice = headphone.Price }
                },
                Payment = new Payment
                {
                    UserId = buyer.Id,
                    Amount = headphone.Price,
                    Method = "VNPay",
                    Status = "Completed",
                    PaidAtUtc = now.AddMinutes(-50)
                }
            };
            dbContext.Orders.Add(order1);
        }

        if (keyboard is not null)
        {
            var order2 = new Order
            {
                BuyerId = buyer.Id,
                AddressId = address.Id,
                OrderDateUtc = now.AddHours(-5),
                TotalPrice = keyboard.Price,
                Status = "Confirmed",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = keyboard.Id, Quantity = 1, UnitPrice = keyboard.Price }
                },
                Payment = new Payment
                {
                    UserId = buyer.Id,
                    Amount = keyboard.Price,
                    Method = "CreditCard",
                    Status = "Completed",
                    PaidAtUtc = now.AddHours(-4)
                },
                ShippingInfo = new ShippingInfo
                {
                    Carrier = "GHN",
                    TrackingNumber = "GHN-2026082400123",
                    Status = "LabelCreated",
                    EstimatedArrivalUtc = now.AddDays(3)
                }
            };
            dbContext.Orders.Add(order2);
        }
    }

    private static async Task SeedCustomPurchaseId5FromId4Async(CloneEbayDbContext dbContext, DateTime now)
    {
        var seller4 = await dbContext.Users.Include(u => u.Store).FirstOrDefaultAsync(u => u.Id == 4);
        var buyer5 = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == 5);

        if (seller4 is null || buyer5 is null) return;

        seller4.Role = UserRole.Seller;
        if (seller4.Store is null)
        {
            seller4.Store = new Store
            {
                SellerId = seller4.Id,
                StoreName = "Kkk Official Store",
                Description = "Cửa hàng chính hãng kkk",
                SellerType = SellerType.Individual,
                LegalName = "kkk Store",
                ContactPhone = "0988888888",
                VerificationStatus = VerificationStatus.Approved,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.Stores.Add(seller4.Store);
        }
        else
        {
            seller4.Store.VerificationStatus = VerificationStatus.Approved;
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync();
        if (category is null) return;

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.SellerId == seller4.Id);
        if (product is null)
        {
            product = new Product
            {
                SellerId = seller4.Id,
                CategoryId = category.Id,
                Title = "iPhone 15 Pro Max 256GB Titan Tự Nhiên",
                Description = "Máy mới 100% nguyên seal, bảo hành chính hãng 12 tháng.",
                Price = 29990000m,
                ImagesJson = "[]",
                Status = ProductStatus.Active,
                CreatedAtUtc = now.AddDays(-3),
                UpdatedAtUtc = now,
                Inventory = new Inventory { Quantity = 10, LastUpdatedUtc = now }
            };
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
        }

        var existingOrder = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.BuyerId == buyer5.Id && o.Items.Any(i => i.ProductId == product.Id));

        if (existingOrder is null)
        {
            var address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == buyer5.Id);
            if (address is null)
            {
                address = new Address
                {
                    UserId = buyer5.Id,
                    FullName = buyer5.Username,
                    Phone = "0912345678",
                    Street = "456 Đường Lê Lợi",
                    City = "Hà Nội",
                    State = "Quận Hoàn Kiếm",
                    Country = "Vietnam",
                    IsDefault = true
                };
                dbContext.Addresses.Add(address);
                await dbContext.SaveChangesAsync();
            }

            var order = new Order
            {
                BuyerId = buyer5.Id,
                AddressId = address.Id,
                OrderDateUtc = now.AddHours(-2),
                TotalPrice = product.Price,
                Status = "Confirmed",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitPrice = product.Price
                    }
                },
                Payment = new Payment
                {
                    UserId = buyer5.Id,
                    Amount = product.Price,
                    Method = "VNPay",
                    Status = "Completed",
                    PaidAtUtc = now.AddHours(-2)
                },
                ShippingInfo = new ShippingInfo
                {
                    Carrier = "Giao Hàng Nhanh",
                    TrackingNumber = "GHN-ID5-ID4-2026",
                    Status = "Delivered",
                    EstimatedArrivalUtc = now.AddHours(-1)
                }
            };
            dbContext.Orders.Add(order);
        }
    }

    private static bool IsTrue(IConfiguration configuration, string key) =>
        bool.TryParse(configuration[key], out var value) && value;
}

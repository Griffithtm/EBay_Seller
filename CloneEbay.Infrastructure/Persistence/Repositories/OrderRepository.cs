using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(CloneEbayDbContext dbContext) : IOrderRepository
{
    public async Task<PagedResult<Order>> GetSellerOrdersAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Items.Any(item => item.Product.SellerId == sellerId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(order => order.OrderDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, page, pageSize, totalItems);
    }

    public async Task<PagedResult<Order>> GetUserOrdersAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.BuyerId == userId || order.Items.Any(item => item.Product.SellerId == userId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(order => order.OrderDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, page, pageSize, totalItems);
    }

    public async Task<Order?> GetSellerOrderByIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.Inventory)
            .Include(order => order.Address)
            .Include(order => order.Payment)
            .Include(order => order.ShippingInfo)
            .SingleOrDefaultAsync(
                order => order.Id == orderId && order.Items.Any(item => item.Product.SellerId == sellerId),
                cancellationToken);
    }

    public async Task<Order?> GetUserOrderByIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.Inventory)
            .Include(order => order.Address)
            .Include(order => order.Payment)
            .Include(order => order.ShippingInfo)
            .SingleOrDefaultAsync(
                order => order.Id == orderId && (order.BuyerId == userId || order.Items.Any(item => item.Product.SellerId == userId)),
                cancellationToken);
    }

    public Task AddShippingInfoAsync(ShippingInfo shippingInfo, CancellationToken cancellationToken = default)
    {
        dbContext.ShippingInfos.Add(shippingInfo);
        return Task.CompletedTask;
    }

    public async Task<ShippingInfo?> GetShippingInfoByOrderIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var hasSellerItem = await dbContext.Orders
            .AsNoTracking()
            .AnyAsync(
                order => order.Id == orderId && order.Items.Any(item => item.Product.SellerId == sellerId),
                cancellationToken);

        if (!hasSellerItem)
        {
            return null;
        }

        return await dbContext.ShippingInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(info => info.OrderId == orderId, cancellationToken);
    }

    public async Task<Payment?> GetPaymentByOrderIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var hasSellerItem = await dbContext.Orders
            .AsNoTracking()
            .AnyAsync(
                order => order.Id == orderId && order.Items.Any(item => item.Product.SellerId == sellerId),
                cancellationToken);

        if (!hasSellerItem)
        {
            return null;
        }

        return await dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);
    }

    public async Task CreateMockOrderAsync(int sellerId, DateTime now, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.SellerId == sellerId, cancellationToken);
        if (product is null) return;

        var buyer = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "seller.pending@cloneebay.local", cancellationToken)
            ?? await dbContext.Users.FirstAsync(u => u.Id == sellerId, cancellationToken);

        var address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == buyer.Id, cancellationToken);
        if (address is null)
        {
            address = new Address
            {
                UserId = buyer.Id,
                FullName = "Demo Buyer",
                Phone = "0987654321",
                Street = "456 Le Loi Street",
                City = "Ho Chi Minh City",
                State = "District 1",
                Country = "Vietnam",
                IsDefault = true
            };
            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var order = new Order
        {
            BuyerId = buyer.Id,
            AddressId = address.Id,
            OrderDateUtc = now,
            TotalPrice = product.Price,
            Status = "Pending",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = product.Price }
            },
            Payment = new Payment
            {
                UserId = buyer.Id,
                Amount = product.Price,
                Method = "VNPay",
                Status = "Completed",
                PaidAtUtc = now
            }
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order> CreateManualOrderAsync(
        int sellerId,
        CreateManualOrderRequest request,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == request.ProductId && p.SellerId == sellerId, cancellationToken);

        if (product is null)
        {
            throw new AppProblemException(400, "INVALID_PRODUCT", "Product not found or does not belong to your store.");
        }

        var buyer = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "seller.pending@cloneebay.local", cancellationToken)
            ?? await dbContext.Users.FirstAsync(u => u.Id == sellerId, cancellationToken);

        var address = new Address
        {
            UserId = buyer.Id,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Street = request.Street.Trim(),
            City = request.City.Trim(),
            State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim(),
            Country = request.Country.Trim(),
            IsDefault = false
        };
        dbContext.Addresses.Add(address);
        await dbContext.SaveChangesAsync(cancellationToken);

        var total = request.Quantity * request.UnitPrice;
        var order = new Order
        {
            BuyerId = buyer.Id,
            AddressId = address.Id,
            OrderDateUtc = now,
            TotalPrice = total,
            Status = "Pending",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice
                }
            },
            Payment = new Payment
            {
                UserId = buyer.Id,
                Amount = total,
                Method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "COD" : request.PaymentMethod,
                Status = string.IsNullOrWhiteSpace(request.PaymentStatus) ? "Pending" : request.PaymentStatus,
                PaidAtUtc = string.Equals(request.PaymentStatus, "Completed", StringComparison.OrdinalIgnoreCase) ? now : null
            }
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }
}

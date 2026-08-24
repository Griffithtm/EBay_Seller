using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
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
}

using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(CloneEbayDbContext dbContext) : IReportRepository
{
    public Task AddDownloadAsync(ReportDownload download, CancellationToken cancellationToken = default) =>
        dbContext.ReportDownloads.AddAsync(download, cancellationToken).AsTask();

    public Task<ReportDownload?> GetDownloadByIdAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        dbContext.ReportDownloads.SingleOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<ReportDownload> Items, int Total)> ListDownloadsAsync(
        int userId, string? source, ReportDownloadStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<ReportDownload> query = dbContext.ReportDownloads.AsNoTracking().Where(d => d.UserId == userId);
        if (!string.IsNullOrWhiteSpace(source)) query = query.Where(d => d.Source == source);
        if (status.HasValue) query = query.Where(d => d.Status == status.Value);
        if (fromUtc.HasValue) query = query.Where(d => d.RequestedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(d => d.RequestedAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.RequestedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> ExistsActiveScheduleAsync(int userId, string source, string type, CancellationToken cancellationToken = default) =>
        dbContext.ReportSchedules.AnyAsync(
            s => s.UserId == userId && s.Source == source && s.Type == type && s.IsActive, cancellationToken);

    public Task AddScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default) =>
        dbContext.ReportSchedules.AddAsync(schedule, cancellationToken).AsTask();

    public Task<ReportSchedule?> GetScheduleByIdAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        dbContext.ReportSchedules.SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<ReportSchedule> Items, int Total)> ListSchedulesAsync(
        int userId, string? source, string? type, ReportScheduleFrequency? frequency, bool onlyActive,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<ReportSchedule> query = dbContext.ReportSchedules.AsNoTracking().Where(s => s.UserId == userId);
        if (!string.IsNullOrWhiteSpace(source)) query = query.Where(s => s.Source == source);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(s => s.Type == type);
        if (frequency.HasValue) query = query.Where(s => s.Frequency == frequency.Value);
        if (onlyActive) query = query.Where(s => s.IsActive);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<ReportSchedule>> GetDueSchedulesAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        await dbContext.ReportSchedules
            .Where(s => s.IsActive && s.NextRunAtUtc != null && s.NextRunAtUtc <= nowUtc &&
                        (s.EndDateUtc == null || s.EndDateUtc >= nowUtc.Date))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductExportRow>> GetListingsForExportAsync(
        int sellerId, ProductStatus[] statuses, int maxRows, CancellationToken cancellationToken = default) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId && statuses.Contains(p.Status))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(maxRows)
            .Select(p => new ProductExportRow(
                p.Id, p.Title, p.Price, p.Inventory.Quantity, p.Status.ToString(), p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderExportRow>> GetOrdersForExportAsync(
        int sellerId, DateTime fromUtc, DateTime toUtc, string[]? statusFilter, int maxRows, CancellationToken cancellationToken = default)
    {
        var sellerOrderIds = dbContext.OrderItems
            .Where(item => item.Product.SellerId == sellerId)
            .Select(item => item.OrderId)
            .Distinct();

        IQueryable<Order> query = dbContext.Orders
            .AsNoTracking()
            .Where(o => sellerOrderIds.Contains(o.Id) && o.OrderDateUtc >= fromUtc && o.OrderDateUtc <= toUtc);
        if (statusFilter is { Length: > 0 })
        {
            query = query.Where(o => statusFilter.Contains(o.Status));
        }

        var orders = await query
            .Include(o => o.Buyer)
            .Include(o => o.Payment)
            .Include(o => o.ShippingInfo)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDateUtc)
            .Take(maxRows)
            .ToListAsync(cancellationToken);

        return orders.Select(o => new OrderExportRow(
            o.Id,
            o.Status,
            o.Buyer.Username,
            o.Buyer.Email,
            o.TotalPrice,
            o.OrderDateUtc,
            o.Payment?.PaidAtUtc,
            o.ShippingInfo?.ShippedAtUtc,
            o.Items.Count,
            string.Join("; ", o.Items.Select(i => $"{i.Product.Title} x{i.Quantity}"))
        )).ToArray();
    }
}

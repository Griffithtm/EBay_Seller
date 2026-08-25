using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class PaymentsReportRepository(CloneEbayDbContext dbContext) : IPaymentsReportRepository
{
    public async Task<PaymentReportRawFigures> GetFiguresAsync(
        int sellerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var sellerOrderIds = dbContext.OrderItems
            .Where(item => item.Product.SellerId == sellerId)
            .Select(item => item.OrderId)
            .Distinct();

        var ordersInRange = dbContext.Orders
            .AsNoTracking()
            .Where(o => sellerOrderIds.Contains(o.Id) && o.OrderDateUtc >= fromUtc && o.OrderDateUtc <= toUtc);

        var grossSales = await ordersInRange
            .Where(o => o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalPrice, cancellationToken) ?? 0m;

        var orderIdsInRange = await ordersInRange.Select(o => o.Id).ToListAsync(cancellationToken);

        var refunds = await dbContext.ReturnRequests
            .AsNoTracking()
            .Where(r => orderIdsInRange.Contains(r.OrderId) && r.Status == ReturnRequestStatus.RefundedByReturn)
            .Join(dbContext.Orders, r => r.OrderId, o => o.Id, (r, o) => (decimal?)o.TotalPrice)
            .SumAsync(cancellationToken) ?? 0m;

        var expenses = await ordersInRange
            .Where(o => o.Status != "Cancelled")
            .SumAsync(o => (decimal?)(o.FinalValueFee > 0 ? o.FinalValueFee : Math.Round(o.TotalPrice * 0.10m, 2)), cancellationToken) ?? 0m;

        var payouts = await dbContext.Payments
            .AsNoTracking()
            .Where(p => orderIdsInRange.Contains(p.OrderId) &&
                        p.Status == "Completed" &&
                        p.PaidAtUtc != null && p.PaidAtUtc >= fromUtc && p.PaidAtUtc <= toUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        return new PaymentReportRawFigures(grossSales, refunds, expenses, payouts);
    }
}

using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class ReturnRepository(CloneEbayDbContext db) : IReturnRepository
{
    public async Task<(IReadOnlyList<ReturnRequest> Items, int Total)> ListBySellerAsync(
        int sellerId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct)
    {
        var q = db.ReturnRequests
            .Include(r => r.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Include(r => r.User)
            .Where(r => r.Order.Items.Any(i => i.Product.SellerId == sellerId))
            .AsQueryable();

        if (fromDate.HasValue) q = q.Where(r => r.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(r => r.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var cleanTerm = term.Replace("#", "");
            var isNumeric = int.TryParse(cleanTerm, out var orderIdNum);
            q = q.Where(r => (isNumeric && (r.Id == orderIdNum || r.OrderId == orderIdNum))
                          || EF.Functions.Like(r.User.Username, $"%{term}%")
                          || EF.Functions.Like(r.Reason, $"%{term}%"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ReturnRequest?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.ReturnRequests
            .Include(r => r.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}

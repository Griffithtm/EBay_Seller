using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class DisputeRepository(CloneEbayDbContext db) : IDisputeRepository
{
    public async Task<(IReadOnlyList<Dispute> Items, int Total)> ListBySellerAsync(
        int sellerId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct)
    {
        var q = db.Disputes
            .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Include(d => d.Raiser)
            .Where(d => d.Order.Items.Any(i => i.Product.SellerId == sellerId))
            .AsQueryable();

        if (fromDate.HasValue) q = q.Where(d => d.Order.OrderDateUtc >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(d => d.Order.OrderDateUtc <= toDate.Value.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var cleanTerm = term.Replace("#", "");
            var isNumeric = int.TryParse(cleanTerm, out var orderIdNum);
            q = q.Where(d => (isNumeric && d.OrderId == orderIdNum)
                          || EF.Functions.Like(d.Raiser.Username, $"%{term}%")
                          || EF.Functions.Like(d.Description, $"%{term}%"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Dispute?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Disputes
            .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Include(d => d.Raiser)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
}

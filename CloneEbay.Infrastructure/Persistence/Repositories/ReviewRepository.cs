using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Application.Reviews;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class ReviewRepository(CloneEbayDbContext db) : IReviewRepository
{
    public async Task<(IReadOnlyList<Review> Items, int Total)> ListAsync(
        int? productId, int? sellerId, bool? hasReply, DateTime? fromDate, DateTime? toDate, string? searchTerm,
        int page, int pageSize, CancellationToken ct)
    {
        var q = db.Reviews
            .Include(r => r.Product)
            .Include(r => r.Reviewer)
            .AsQueryable();

        if (productId.HasValue) q = q.Where(r => r.ProductId == productId.Value);
        if (sellerId.HasValue) q = q.Where(r => r.Product.SellerId == sellerId.Value);
        if (fromDate.HasValue) q = q.Where(r => r.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(r => r.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var cleanTerm = term.Replace("#", "");
            var isNumeric = int.TryParse(cleanTerm, out var reviewIdNum);
            q = q.Where(r => (isNumeric && r.Id == reviewIdNum)
                         || EF.Functions.Like(r.Product.Title, $"%{term}%") 
                         || EF.Functions.Like(r.Reviewer.Username, $"%{term}%") 
                         || EF.Functions.Like(r.Comment, $"%{term}%"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Review?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Reviews
            .Include(r => r.Product)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public void Add(Review review) => db.Reviews.Add(review);

    public async Task<Feedback?> GetFeedbackBySellerIdAsync(int sellerId, CancellationToken ct) =>
        await db.Feedbacks.FirstOrDefaultAsync(f => f.SellerId == sellerId, ct);

    public async Task<List<int>> GetRatingsBySellerIdAsync(int sellerId, CancellationToken ct) =>
        await db.Reviews
            .Where(r => r.Product.SellerId == sellerId)
            .Select(r => r.Rating)
            .ToListAsync(ct);

    public void AddFeedback(Feedback feedback) => db.Feedbacks.Add(feedback);

    public async Task<BuyerSummaryResponse> GetBuyerSummaryAsync(int sellerId, int buyerId, CancellationToken ct)
    {
        var buyer = await db.Users.FirstOrDefaultAsync(u => u.Id == buyerId, ct)
            ?? throw new AppProblemException(404, "BUYER_NOT_FOUND", "Buyer not found.");

        var buyerReviews = await db.Reviews
            .Include(r => r.Product)
            .Where(r => r.ReviewerId == buyerId && r.Product.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var buyerReturns = await db.ReturnRequests
            .Include(r => r.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Where(r => r.UserId == buyerId && r.Order.Items.Any(i => i.Product.SellerId == sellerId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var avgRating = buyerReviews.Count > 0
            ? Math.Round(buyerReviews.Average(r => (decimal)r.Rating), 1)
            : 0m;

        var allSellerReviews = await db.Reviews
            .Where(r => r.Product.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var allSellerReturns = await db.ReturnRequests
            .Where(r => r.Order.Items.Any(i => i.Product.SellerId == sellerId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var reviewSummaries = buyerReviews.Select(r => {
            var index = allSellerReviews.IndexOf(r.Id);
            var page = index >= 0 ? (index / 7) + 1 : 1;
            var statusStr = r.Response != null ? "Đã phản hồi" : "Chưa phản hồi";
            return new BuyerItemSummary(r.Id, 0, r.CreatedAt, r.Product.Title, r.Rating, statusStr, page);
        }).ToList();

        var returnSummaries = buyerReturns.Select(ret => {
            var index = allSellerReturns.IndexOf(ret.Id);
            var page = index >= 0 ? (index / 7) + 1 : 1;
            var item = ret.Order.Items.FirstOrDefault(i => i.Product.SellerId == sellerId);
            var title = item?.Product?.Title ?? $"Đơn hàng #{ret.OrderId}";
            return new BuyerItemSummary(ret.Id, ret.OrderId, ret.CreatedAt, title, 0, ret.Status, page);
        }).ToList();

        return new BuyerSummaryResponse(buyer.Id, buyer.Username, buyer.Email, buyerReviews.Count, buyerReturns.Count, avgRating, reviewSummaries, returnSummaries);
    }
}

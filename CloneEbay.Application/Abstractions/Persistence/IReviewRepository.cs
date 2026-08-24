using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IReviewRepository
{
    Task<(IReadOnlyList<Review> Items, int Total)> ListAsync(
        int? productId, int? sellerId, bool? hasReply, DateTime? fromDate, DateTime? toDate, string? searchTerm,
        int page, int pageSize, CancellationToken ct);

    Task<Review?> GetByIdAsync(int id, CancellationToken ct);
    void Add(Review review);
    Task<Feedback?> GetFeedbackBySellerIdAsync(int sellerId, CancellationToken ct);
    Task<List<int>> GetRatingsBySellerIdAsync(int sellerId, CancellationToken ct);
    void AddFeedback(Feedback feedback);
    Task<Reviews.BuyerSummaryResponse> GetBuyerSummaryAsync(int sellerId, int buyerId, CancellationToken ct);
}


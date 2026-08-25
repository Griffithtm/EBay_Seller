using CloneEbay.Application.Common;

namespace CloneEbay.Application.Reviews;

public interface IReviewService
{
    Task<PagedResult<ReviewResponse>> GetReviewsAsync(ReviewListQuery query, CancellationToken ct = default);
    Task<ReviewResponse> ReplyToReviewAsync(int reviewId, int sellerUserId, string response, CancellationToken ct = default);
    Task<ReviewResponse> RequestReviewRevisionAsync(int reviewId, int sellerUserId, CancellationToken ct = default);
    Task<ReviewResponse> ApproveReviewRevisionAsync(int reviewId, int buyerUserId, string? newComment, int? newRating, CancellationToken ct = default);
    Task<ReviewResponse> RejectReviewRevisionAsync(int reviewId, int buyerUserId, CancellationToken ct = default);
    Task<FeedbackResponse> GetFeedbackAsync(int sellerId, CancellationToken ct = default);
    Task RecalculateFeedbackAsync(int sellerId, CancellationToken ct = default);
    Task<BuyerSummaryResponse> GetBuyerSummaryAsync(int sellerId, int buyerId, CancellationToken ct = default);
}

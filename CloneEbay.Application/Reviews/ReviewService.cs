using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Reviews;

public sealed class ReviewService(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IReviewService
{
    public async Task<PagedResult<ReviewResponse>> GetReviewsAsync(ReviewListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await reviewRepository.ListAsync(
            query.ProductId, query.SellerId, query.HasReply, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);

        var dtos = items.Select(Map).ToList();
        return new PagedResult<ReviewResponse>(dtos, query.Page, query.PageSize, total);
    }

    public async Task<ReviewResponse> ReplyToReviewAsync(int reviewId, int sellerUserId, string response, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(reviewId, ct)
            ?? throw new AppProblemException(404, "REVIEW_NOT_FOUND", "Review not found.");

        if (review.Product.SellerId != sellerUserId)
            throw new AppProblemException(403, "FORBIDDEN_NOT_OWNER", "You do not own the product for this review.");

        if (review.Response is not null)
            throw new AppProblemException(409, "REVIEW_ALREADY_REPLIED",
                "You have already replied to this review. Replies cannot be changed.");

        review.Response = response;
        review.RepliedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        return Map(review);
    }

    public async Task<ReviewResponse> RequestReviewRevisionAsync(int reviewId, int sellerUserId, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(reviewId, ct)
            ?? throw new AppProblemException(404, "REVIEW_NOT_FOUND", "Review not found.");

        if (review.Product.SellerId != sellerUserId)
            throw new AppProblemException(403, "FORBIDDEN_NOT_OWNER", "You do not own the product for this review.");

        if (review.RevisionStatus == ReviewRevisionStatus.Pending)
            throw new AppProblemException(409, "REVISION_ALREADY_REQUESTED", "Revision request is already pending.");

        review.RevisionStatus = ReviewRevisionStatus.Pending;
        review.RevisionRequestedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        return Map(review);
    }

    public async Task<ReviewResponse> ApproveReviewRevisionAsync(int reviewId, int buyerUserId, string? newComment, int? newRating, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(reviewId, ct)
            ?? throw new AppProblemException(404, "REVIEW_NOT_FOUND", "Review not found.");

        if (review.ReviewerId != buyerUserId)
            throw new AppProblemException(403, "FORBIDDEN_NOT_REVIEWER", "You are not the reviewer of this item.");

        if (review.RevisionStatus != ReviewRevisionStatus.Pending)
            throw new AppProblemException(400, "INVALID_REVISION_STATUS", "No pending revision request found.");

        if (newRating.HasValue)
        {
            if (newRating.Value < 1 || newRating.Value > 5)
                throw new AppProblemException(400, "INVALID_RATING", "Rating must be between 1 and 5.");
            review.Rating = newRating.Value;
            review.RatingType = ReviewRatingType.FromRating(newRating.Value);
        }

        if (!string.IsNullOrWhiteSpace(newComment))
        {
            review.Comment = newComment.Trim();
        }

        review.RevisionStatus = ReviewRevisionStatus.Approved;
        await unitOfWork.SaveChangesAsync(ct);

        await RecalculateFeedbackAsync(review.Product.SellerId, ct);

        return Map(review);
    }

    public async Task<ReviewResponse> RejectReviewRevisionAsync(int reviewId, int buyerUserId, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(reviewId, ct)
            ?? throw new AppProblemException(404, "REVIEW_NOT_FOUND", "Review not found.");

        if (review.ReviewerId != buyerUserId)
            throw new AppProblemException(403, "FORBIDDEN_NOT_REVIEWER", "You are not the reviewer of this item.");

        if (review.RevisionStatus != ReviewRevisionStatus.Pending)
            throw new AppProblemException(400, "INVALID_REVISION_STATUS", "No pending revision request found.");

        review.RevisionStatus = ReviewRevisionStatus.Rejected;
        await unitOfWork.SaveChangesAsync(ct);

        return Map(review);
    }

    public async Task<FeedbackResponse> GetFeedbackAsync(int sellerId, CancellationToken ct = default)
    {
        var ratings = await reviewRepository.GetRatingsBySellerIdAsync(sellerId, ct);
        var total = ratings.Count;
        if (total == 0)
        {
            return new FeedbackResponse(sellerId, 0m, 0, 0m);
        }

        var avg = Math.Round(ratings.Average(r => (decimal)r), 1);
        var positiveRate = Math.Round((decimal)ratings.Count(r => r >= 4) / total * 100, 1);

        return new FeedbackResponse(sellerId, avg, total, positiveRate);
    }

    public async Task RecalculateFeedbackAsync(int sellerId, CancellationToken ct = default)
    {
        var ratings = await reviewRepository.GetRatingsBySellerIdAsync(sellerId, ct);
        var total = ratings.Count;
        if (total == 0) return;

        var avg = Math.Round(ratings.Average(r => (decimal)r), 2);
        var positiveRate = Math.Round((decimal)ratings.Count(r => r >= 4) / total * 100, 2);

        var feedback = await reviewRepository.GetFeedbackBySellerIdAsync(sellerId, ct);
        if (feedback is null)
        {
            reviewRepository.AddFeedback(new Feedback
            {
                SellerId = sellerId,
                AverageRating = avg,
                TotalReviews = total,
                PositiveRate = positiveRate
            });
        }
        else
        {
            feedback.AverageRating = avg;
            feedback.TotalReviews = total;
            feedback.PositiveRate = positiveRate;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task<BuyerSummaryResponse> GetBuyerSummaryAsync(int sellerId, int buyerId, CancellationToken ct = default) =>
        reviewRepository.GetBuyerSummaryAsync(sellerId, buyerId, ct);

    private static ReviewResponse Map(Review r) => new(
        r.Id,
        r.ProductId,
        r.Product?.Title ?? string.Empty,
        r.ReviewerId,
        r.Reviewer?.Username ?? string.Empty,
        r.RecipientId,
        r.ReviewerRole ?? ReviewParticipantRole.Buyer,
        r.RecipientRole ?? ReviewParticipantRole.Seller,
        r.Rating,
        r.RatingType ?? ReviewRatingType.FromRating(r.Rating),
        r.Comment,
        r.Response,
        r.RepliedAt,
        r.RevisionStatus ?? ReviewRevisionStatus.None,
        r.RevisionRequestedAt,
        r.CreatedAt);
}

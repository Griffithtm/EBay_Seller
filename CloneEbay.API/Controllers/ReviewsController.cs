using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<PagedResult<ReviewResponse>>>> GetReviews(
        [FromQuery] int? productId,
        [FromQuery] int? sellerId,
        [FromQuery] bool? hasReply,
        [FromQuery] string? ratingType,
        [FromQuery] string? revisionStatus,
        DateTime? fromDate, DateTime? toDate, string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new ReviewListQuery(productId, sellerId, hasReply, ratingType, revisionStatus, fromDate, toDate, searchTerm, page, pageSize);
        var result = await reviewService.GetReviewsAsync(query, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<ReviewResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/reply")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ReviewResponse>>> ReplyToReview(
        int id,
        [FromBody] ReplyToReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.ReplyToReviewAsync(id, User.GetRequiredUserId(), request.Response, cancellationToken);
        await reviewService.RecalculateFeedbackAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ReviewResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/request-revision")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ReviewResponse>>> RequestReviewRevision(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.RequestReviewRevisionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ReviewResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/approve-revision")]
    [Authorize]
    public async Task<ActionResult<ApiEnvelope<ReviewResponse>>> ApproveReviewRevision(
        int id,
        [FromBody] UpdateReviewRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.ApproveReviewRevisionAsync(id, User.GetRequiredUserId(), request.NewComment, request.NewRating, cancellationToken);
        return Ok(ApiEnvelope<ReviewResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/reject-revision")]
    [Authorize]
    public async Task<ActionResult<ApiEnvelope<ReviewResponse>>> RejectReviewRevision(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.RejectReviewRevisionAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ReviewResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[Route("api/feedback")]
public class FeedbackController(IReviewService reviewService) : ControllerBase
{
    [HttpGet("{sellerId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<FeedbackResponse>>> GetFeedback(
        int sellerId,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.GetFeedbackAsync(sellerId, cancellationToken);
        return Ok(ApiEnvelope<FeedbackResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

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
        [FromQuery] bool? hasReply, DateTime? fromDate, DateTime? toDate, string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new ReviewListQuery(productId, sellerId, hasReply, fromDate, toDate, searchTerm, page, pageSize);
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
        // Fire and forget or await? Awaiting for simplicity to ensure consistency
        await reviewService.RecalculateFeedbackAsync(User.GetRequiredUserId(), cancellationToken);
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


using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class ReturnRequest : BaseEntity
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreferredResolution { get; set; } = "Refund"; // Refund, Replacement
    public string Status { get; set; } = ReturnRequestStatus.Requested;
    public string? ReturnCarrier { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? BuyerShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public decimal? RestockingFee { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? SellerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User User { get; set; } = null!;
}

public static class ReturnRequestStatus
{
    public const string Requested = "Requested";
    public const string Accepted = "Accepted";
    public const string AwaitingBuyerReturn = "AwaitingBuyerReturn";
    public const string InTransitBackToSeller = "InTransitToSeller";
    public const string DeliveredToSeller = "DeliveredToSeller";
    public const string RefundOffered = "RefundOffered";
    public const string RefundedByReturn = "RefundedByReturn";
    public const string ReplacementSent = "ReplacementSent";
    public const string Declined = "Declined";
    public const string Closed = "Closed";

    private static readonly Dictionary<string, IReadOnlySet<string>> ValidTransitions = new()
    {
        [Requested] = new HashSet<string> { Accepted, AwaitingBuyerReturn, RefundOffered, Declined },
        [Accepted] = new HashSet<string> { AwaitingBuyerReturn, RefundedByReturn, ReplacementSent },
        [AwaitingBuyerReturn] = new HashSet<string> { InTransitBackToSeller, DeliveredToSeller, RefundedByReturn },
        [InTransitBackToSeller] = new HashSet<string> { DeliveredToSeller, RefundedByReturn },
        [DeliveredToSeller] = new HashSet<string> { RefundedByReturn, ReplacementSent, Closed },
        [RefundOffered] = new HashSet<string> { Closed, RefundedByReturn },
        [RefundedByReturn] = new HashSet<string> { Closed },
        [ReplacementSent] = new HashSet<string> { Closed },
        [Declined] = new HashSet<string> { Closed },
        [Closed] = new HashSet<string>()
    };

    public static bool IsValidTransition(string from, string to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

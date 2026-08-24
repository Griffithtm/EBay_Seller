using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class ReturnRequest : BaseEntity
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = ReturnRequestStatus.Requested;
    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User User { get; set; } = null!;
}

public static class ReturnRequestStatus
{
    public const string Requested = "Requested";
    public const string Accepted = "Accepted";
    public const string RefundOffered = "RefundOffered";
    public const string Declined = "Declined";
    public const string RefundedByReturn = "RefundedByReturn";
    public const string Closed = "Closed";

    private static readonly Dictionary<string, IReadOnlySet<string>> ValidTransitions = new()
    {
        [Requested] = new HashSet<string> { Accepted, RefundOffered, Declined },
        [Accepted] = new HashSet<string> { RefundedByReturn },
        [RefundOffered] = new HashSet<string> { Closed },
        [RefundedByReturn] = new HashSet<string> { Closed },
        [Declined] = new HashSet<string> { Closed },
        [Closed] = new HashSet<string>()
    };

    public static bool IsValidTransition(string from, string to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class ReturnPolicy : BaseEntity
{
    public int StoreId { get; set; }
    public bool AcceptReturns { get; set; } = true;
    public int ReturnPeriodDays { get; set; } = 30; // 14, 30, 60
    public string ReturnShippingPaidBy { get; set; } = ReturnShippingPaidByOptions.Buyer; // "Buyer" or "Seller"
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;
}

public static class ReturnShippingPaidByOptions
{
    public const string Buyer = "Buyer";
    public const string Seller = "Seller";
}

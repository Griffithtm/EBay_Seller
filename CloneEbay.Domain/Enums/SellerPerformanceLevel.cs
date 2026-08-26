namespace CloneEbay.Domain.Enums;

/// <summary>
/// Seller performance tier, re-evaluated automatically from the seller's trailing
/// 12-month operational metrics. This is independent from <see cref="SellerType"/>
/// (Individual/Business) and from any paid Store subscription tier: it is a
/// system-computed quality score, not something the seller can choose.
/// </summary>
public enum SellerPerformanceLevel
{
    BelowStandard = 0,
    AboveStandard = 1,
    TopRated = 2
}

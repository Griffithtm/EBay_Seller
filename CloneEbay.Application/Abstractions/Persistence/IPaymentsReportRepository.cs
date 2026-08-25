namespace CloneEbay.Application.Abstractions.Persistence;

public sealed record PaymentReportRawFigures(decimal GrossSales, decimal Refunds, decimal Expenses, decimal Payouts);

public interface IPaymentsReportRepository
{
    /// <summary>
    /// GrossSales: sum of Order.TotalPrice for the seller's non-cancelled orders in range.
    /// Refunds: sum of Order.TotalPrice for orders with a return in RefundedByReturn status.
    /// Expenses: sum of Order.FinalValueFee (10% eBay platform fee) for non-cancelled orders.
    /// Payouts: sum of Payment.Amount where Payment.Status == "Completed" and PaidAt in range.
    /// </summary>
    Task<PaymentReportRawFigures> GetFiguresAsync(int sellerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

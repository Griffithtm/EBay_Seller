namespace CloneEbay.Application.Abstractions.Persistence;

public sealed record PaymentReportRawFigures(decimal GrossSales, decimal Refunds, decimal Payouts);

public interface IPaymentsReportRepository
{
    /// <summary>
    /// GrossSales: sum of Order.TotalPrice for the seller's non-cancelled orders in range.
    /// Refunds: sum of Order.TotalPrice for orders with a return in RefundedByReturn status
    /// (real data — this project tracks that, unlike the reference project's B.5 gap).
    /// Payouts: sum of Payment.Amount where Payment.Status == "Completed" and PaidAtUtc in
    /// range (real settled-payment data, not the reference project's synthetic PaidAt+2d formula).
    /// </summary>
    Task<PaymentReportRawFigures> GetFiguresAsync(int sellerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

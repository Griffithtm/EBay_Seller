using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;

namespace CloneEbay.Application.Payments;

/// <summary>
/// Implements spec Part B, adapted to this project's actual schema (no PlatformFee,
/// ShippingCost, or discount tracking on Order — see the codebase-scoping note recorded
/// alongside this file). The period/compare whitelist, date-range math, and rounding rule
/// are ported faithfully (spec says these are "đạt chuẩn"); the 5-section financial
/// breakdown is intentionally NOT copied because most of its source fields don't exist
/// here — sections without a real data source report 0 with an explicit <c>Note</c>
/// instead of silently showing a number that looks real but isn't (this is what spec B.5
/// explicitly asks to avoid).
/// </summary>
public sealed class PaymentsReportService(IPaymentsReportRepository repository, IClock clock) : IPaymentsReportService
{
    private const string DataGapNote =
        "Expenses (platform fee) và Charges hiển thị 0: project chưa có mô hình phí sàn/phí vận chuyển tách " +
        "riêng khỏi Order.TotalPrice. Gross sales, Refunds, và Payouts là số liệu thật từ Order/ReturnRequest/Payment.";

    public async Task<PaymentReportResponse> GetReportAsync(int sellerId, PaymentReportFilter filter, CancellationToken cancellationToken = default)
    {
        if (!PaymentReportPeriodResolver.ValidPeriods.Contains(filter.Period))
        {
            throw new AppProblemException(422, "INVALID_PAYMENT_REPORT_PERIOD", $"'{filter.Period}' is not a supported period.");
        }
        if (!PaymentReportPeriodResolver.ValidCompared.Contains(filter.Compared))
        {
            throw new AppProblemException(422, "INVALID_PAYMENT_REPORT_COMPARED", $"'{filter.Compared}' is not a supported comparison.");
        }

        var now = clock.UtcNow;
        var (fromUtc, toUtc) = PaymentReportPeriodResolver.Resolve(filter.Period, now);
        var current = await BuildPeriodAsync(sellerId, fromUtc, toUtc, cancellationToken);

        PaymentReportPeriodDto? comparison = null;
        if (!filter.Compared.Equals(PaymentReportCompared.None, StringComparison.OrdinalIgnoreCase))
        {
            var (compFromUtc, compToUtc) = PaymentReportPeriodResolver.ResolveComparison(filter.Compared, fromUtc, toUtc);
            comparison = await BuildPeriodAsync(sellerId, compFromUtc, compToUtc, cancellationToken);
        }

        return new PaymentReportResponse("VND", current, comparison);
    }

    private async Task<PaymentReportPeriodDto> BuildPeriodAsync(int sellerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var raw = await repository.GetFiguresAsync(sellerId, fromUtc, toUtc, cancellationToken);
        const decimal expenses = 0m;
        const decimal charges = 0m;
        var figures = new PaymentReportSectionDto(
            GrossSales: PaymentReportPeriodResolver.RoundMoney(raw.GrossSales),
            Refunds: PaymentReportPeriodResolver.RoundMoney(raw.Refunds),
            Expenses: expenses,
            Charges: charges,
            Payouts: PaymentReportPeriodResolver.RoundMoney(raw.Payouts),
            NetTransfersTotal: PaymentReportPeriodResolver.RoundMoney(raw.Payouts - charges));
        return new PaymentReportPeriodDto(fromUtc, toUtc, figures, DataGapNote);
    }
}

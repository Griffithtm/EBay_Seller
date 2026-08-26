namespace CloneEbay.Application.Payments;

public interface IPaymentsReportService
{
    Task<PaymentReportResponse> GetReportAsync(int sellerId, PaymentReportFilter filter, CancellationToken cancellationToken = default);
}

namespace CloneEbay.Application.SaleEvents;

public interface ISaleEventService
{
    Task<IReadOnlyList<SaleEventSummaryResponse>> GetForSellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<SaleEventDetailResponse?> GetByIdAsync(int sellerId, int id, CancellationToken cancellationToken = default);
    Task<SaleEventDetailResponse> CreateAsync(int sellerId, CreateSaleEventRequest request, CancellationToken cancellationToken = default);
    Task<SaleEventDetailResponse> UpdateAsync(int sellerId, int id, UpdateSaleEventRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int sellerId, int id, string status, CancellationToken cancellationToken = default);
    Task DeleteAsync(int sellerId, int id, CancellationToken cancellationToken = default);
    Task<EligibleProductsPageResponse> GetEligibleProductsAsync(
        int sellerId,
        int? excludeSaleEventId,
        EligibleProductQuery query,
        CancellationToken cancellationToken = default);
}

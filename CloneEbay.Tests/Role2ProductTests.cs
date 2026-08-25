using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Products;
using CloneEbay.Application.Sellers;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class Role2ProductTests
{
    [Fact]
    public async Task Close_auction_selects_earliest_high_bid_and_creates_one_order()
    {
        var product = AuctionProduct();
        product.Bids.Add(new Bid { Id = 1, ProductId = 10, BidderId = 21, Amount = 500, BidTimeUtc = Utc(10, 5) });
        product.Bids.Add(new Bid { Id = 2, ProductId = 10, BidderId = 22, Amount = 500, BidTimeUtc = Utc(10, 3) });
        var repository = new FakeProducts(product);
        var service = new ProductService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeSellerPerformanceService());

        var result = await service.CloseAuctionAsync(10, 4, false);

        Assert.Equal(22, result.WinnerId);
        Assert.Equal(500, result.WinningAmount);
        Assert.Equal(900, result.OrderId);
        Assert.False(result.OrderCreationPending);
        var order = Assert.Single(repository.Orders);
        Assert.Equal("WaitingPayment", order.Status);
        Assert.Equal(22, order.BuyerId);
        Assert.Equal(10, order.AuctionProductId);
        Assert.Equal(500, Assert.Single(order.Items).UnitPrice);
        Assert.Equal(0, product.Inventory.Quantity);
    }

    [Fact]
    public async Task Close_auction_is_idempotent_and_does_not_duplicate_order()
    {
        var product = AuctionProduct();
        product.Bids.Add(new Bid { Id = 1, ProductId = 10, BidderId = 21, Amount = 600, BidTimeUtc = Utc(10, 2) });
        var repository = new FakeProducts(product);
        var service = new ProductService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeSellerPerformanceService());

        var first = await service.CloseAuctionAsync(10, 4, false);
        var second = await service.CloseAuctionAsync(10, 4, false);

        Assert.Equal(first.OrderId, second.OrderId);
        Assert.Single(repository.Orders);
    }

    [Fact]
    public async Task Close_auction_without_bids_creates_no_order()
    {
        var product = AuctionProduct();
        var repository = new FakeProducts(product);
        var service = new ProductService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeSellerPerformanceService());

        var result = await service.CloseAuctionAsync(10, 4, false);

        Assert.Null(result.WinnerId);
        Assert.Null(result.OrderId);
        Assert.Empty(repository.Orders);
        Assert.Equal(ProductStatus.AuctionEnded, product.Status);
    }

    [Fact]
    public async Task Delete_product_with_order_returns_expected_conflict()
    {
        var product = AuctionProduct();
        var repository = new FakeProducts(product) { ProductHasOrders = true };
        var service = new ProductService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeSellerPerformanceService());

        var problem = await Assert.ThrowsAsync<AppProblemException>(() => service.DeleteAsync(10, 4));

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("PRODUCT_HAS_ORDERS", problem.ErrorCode);
        Assert.False(repository.WasRemoved);
    }

    [Fact]
    public async Task Bid_must_exceed_current_highest_bid()
    {
        var product = AuctionProduct(endTime: Utc(13, 0));
        product.Bids.Add(new Bid { Id = 1, ProductId = 10, BidderId = 21, Amount = 500, BidTimeUtc = Utc(10, 2) });
        var service = new ProductService(new FakeProducts(product), new FakeUnitOfWork(), new FixedClock(), new FakeSellerPerformanceService());

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.PlaceBidAsync(10, 23, new PlaceBidRequest(500)));

        Assert.Equal("BID_TOO_LOW", problem.ErrorCode);
    }

    private static Product AuctionProduct(DateTime? endTime = null) => new()
    {
        Id = 10,
        SellerId = 4,
        Title = "Auction product",
        Description = "Auction product description",
        Price = 100,
        CategoryId = 1,
        IsAuction = true,
        AuctionEndTimeUtc = endTime ?? Utc(11, 0),
        Status = ProductStatus.Active,
        Inventory = new Inventory { Id = 3, ProductId = 10, Quantity = 1 }
    };

    private static DateTime Utc(int hour, int minute) => new(2026, 8, 23, hour, minute, 0, DateTimeKind.Utc);

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Utc(12, 0);
    }

    private sealed class FakeProducts(Product product) : IProductRepository
    {
        public List<Order> Orders { get; } = [];
        public bool ProductHasOrders { get; init; }
        public bool WasRemoved { get; private set; }
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Product?> GetByIdAsync(int productId, bool tracked, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(product.Id == productId ? product : null);
        public Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(product.Id == productId ? product : null);
        public Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(
            int? sellerId, ProductStatus? status, int? categoryId, string? keyword, bool includeHidden,
            string? sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Product>)[product], 1));
        public Task AddAsync(Product value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Product value) => WasRemoved = true;
        public Task AddBidAsync(Bid bid, CancellationToken cancellationToken = default)
        {
            bid.Id = product.Bids.Count + 1;
            product.Bids.Add(bid);
            return Task.CompletedTask;
        }
        public Task<bool> HasOrderItemsAsync(int productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductHasOrders);
        public Task<Order?> GetAuctionOrderAsync(int productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Orders.SingleOrDefault(order => order.AuctionProductId == productId));
        public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.Id = 900 + Orders.Count;
            Orders.Add(order);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<int>> GetExpiredAuctionIdsAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>([]);
    }

    /// <summary>No-op stub: these tests never call CreateAsync, so the selling-limit gate is never exercised.</summary>
    private sealed class FakeSellerPerformanceService : ISellerPerformanceService
    {
        public Task<SellerPerformanceResponse> GetAsync(int sellerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<SellerPerformanceWarningResponse> GetEarlyWarningAsync(int sellerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task EvaluateSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<int> EvaluateDueSellersAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task EnsureCanListAsync(int sellerId, decimal newListingEstimatedValue, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<SellerPerformanceResponse> OverrideLevelAsync(int sellerId, string level, int adminUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}

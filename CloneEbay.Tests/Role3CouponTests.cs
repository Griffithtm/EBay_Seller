using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Coupons;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Tests;

public sealed class Role3CouponTests
{
    [Fact]
    public async Task Create_coupon_blocked_when_recaptcha_fails()
    {
        var repository = new FakeCoupons();
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: false));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest()));

        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("RECAPTCHA_VERIFICATION_FAILED", problem.ErrorCode);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Create_coupon_rejects_non_owner_product()
    {
        var repository = new FakeCoupons { ProductSellerId = 9 };
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest()));

        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("FORBIDDEN_NOT_OWNER", problem.ErrorCode);
    }

    [Fact]
    public async Task Create_coupon_rejects_duplicate_code_for_same_product()
    {
        var repository = new FakeCoupons { ProductSellerId = 4, CodeAlreadyExists = true };
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest()));

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("COUPON_CODE_ALREADY_EXISTS", problem.ErrorCode);
    }

    [Fact]
    public async Task Create_coupon_normalizes_code_and_persists()
    {
        var repository = new FakeCoupons { ProductSellerId = 4 };
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var result = await service.CreateAsync(4, CreateRequest(code: " summer10 "));

        Assert.Equal("SUMMER10", result.Code);
        Assert.Equal(0, result.UsageCount);
        Assert.Equal("Active", result.Status);
        Assert.Single(repository.Added);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Create_coupon_rejects_invalid_discount(decimal discount)
    {
        var repository = new FakeCoupons { ProductSellerId = 4 };
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest(discountPercent: discount)));

        Assert.Equal("COUPON_DISCOUNT_INVALID", problem.ErrorCode);
    }

    [Fact]
    public async Task Validate_returns_expired_after_end_date()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(2));
        var repository = new FakeCoupons(coupon);
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId);

        Assert.False(result.IsValid);
        Assert.Equal("EXPIRED", result.Reason);
    }

    [Fact]
    public async Task Validate_returns_usage_limit_reached_when_exhausted()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.MaxUsage = 2;
        coupon.UsageCount = 2;
        var repository = new FakeCoupons(coupon);
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId);

        Assert.False(result.IsValid);
        Assert.Equal("USAGE_LIMIT_REACHED", result.Reason);
    }

    [Fact]
    public async Task Validate_returns_valid_with_remaining_usage()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.MaxUsage = 5;
        coupon.UsageCount = 2;
        var repository = new FakeCoupons(coupon);
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.RemainingUsage);
        Assert.Equal(coupon.DiscountPercent, result.DiscountPercent);
    }

    [Fact]
    public async Task Delete_rejects_coupon_already_used()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.UsageCount = 1;
        var repository = new FakeCoupons(coupon) { ProductSellerId = 4 };
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() => service.DeleteAsync(coupon.Id, 4));

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("COUPON_ALREADY_USED", problem.ErrorCode);
        Assert.False(repository.WasRemoved);
    }

    [Fact]
    public async Task Delete_rejects_when_not_owner()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        var repository = new FakeCoupons(coupon);
        var service = new CouponService(repository, new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(success: true));

        var problem = await Assert.ThrowsAsync<AppProblemException>(() => service.DeleteAsync(coupon.Id, 999));

        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("FORBIDDEN_NOT_OWNER", problem.ErrorCode);
    }

    private static CreateCouponRequest CreateRequest(string code = "SUMMER10", decimal discountPercent = 10) =>
        new(ProductId: 10, code, discountPercent, Utc(1), Utc(20), MaxUsage: 5, RecaptchaToken: "token");

    private static Coupon SampleCoupon(DateTime start, DateTime end) => new()
    {
        Id = 1,
        ProductId = 10,
        Code = "SUMMER10",
        DiscountPercent = 10,
        StartDateUtc = start,
        EndDateUtc = end,
        MaxUsage = 5,
        UsageCount = 0,
        CreatedAtUtc = start,
        UpdatedAtUtc = start,
        Product = new Product { Id = 10, SellerId = 4, Title = "Sample product" }
    };

    private static DateTime Utc(int day) => new(2026, 8, day, 0, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Utc(10);
    }

    private sealed class FakeRecaptchaVerifier(bool success) : IRecaptchaVerifier
    {
        public Task<RecaptchaVerificationResult> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RecaptchaVerificationResult(success, success ? 0.9 : 0.1, success ? null : "LOW_SCORE"));
    }

    private sealed class FakeCoupons(Coupon? coupon = null) : ICouponRepository
    {
        public List<Coupon> Added { get; } = [];
        public bool WasRemoved { get; private set; }
        public int ProductSellerId { get; init; } = 4;
        public bool CodeAlreadyExists { get; init; }

        public Task<Coupon?> GetByIdAsync(int couponId, bool tracked, CancellationToken cancellationToken = default) =>
            Task.FromResult(coupon?.Id == couponId ? coupon : null);

        public Task<Coupon?> GetByCodeForProductAsync(string code, int productId, bool tracked, CancellationToken cancellationToken = default) =>
            Task.FromResult(coupon is not null && coupon.ProductId == productId && coupon.Code == code ? coupon : null);

        public Task<bool> CodeExistsForProductAsync(string code, int productId, int? excludeCouponId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CodeAlreadyExists);

        public Task<IReadOnlyList<Coupon>> ListAsync(int? sellerId, int? productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Coupon>>(coupon is null ? [] : [coupon]);

        public Task<int?> GetProductSellerIdAsync(int productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<int?>(ProductSellerId);

        public Task AddAsync(Coupon value, CancellationToken cancellationToken = default)
        {
            value.Id = Added.Count + 1;
            value.Product = new Product { Id = value.ProductId, SellerId = ProductSellerId, Title = "Sample product" };
            Added.Add(value);
            return Task.CompletedTask;
        }

        public void Remove(Coupon value) => WasRemoved = true;
    }
}

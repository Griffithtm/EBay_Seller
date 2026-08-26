using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Coupons;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class Role3CouponTests
{
    [Fact]
    public async Task Create_coupon_blocked_when_recaptcha_fails()
    {
        var repository = new FakeCoupons();
        var service = NewService(repository, recaptchaSuccess: false);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest()));

        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("RECAPTCHA_VERIFICATION_FAILED", problem.ErrorCode);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Create_coupon_rejects_duplicate_code()
    {
        var repository = new FakeCoupons { CodeAlreadyExists = true };
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest()));

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("COUPON_CODE_ALREADY_EXISTS", problem.ErrorCode);
    }

    [Fact]
    public async Task Create_coupon_normalizes_code_and_persists()
    {
        var repository = new FakeCoupons();
        var service = NewService(repository);

        var result = await service.CreateAsync(4, CreateRequest(code: " summer10 "));

        Assert.Equal("SUMMER10", result.Code);
        Assert.Equal(0, result.UsedCount);
        Assert.True(result.IsActive);
        Assert.Single(repository.Added);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_coupon_rejects_invalid_discount(decimal discountValue)
    {
        var repository = new FakeCoupons();
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest(discountValue: discountValue)));

        Assert.Equal("COUPON_DISCOUNT_INVALID", problem.ErrorCode);
    }

    [Fact]
    public async Task Create_coupon_rejects_wrong_discount_unit_for_type()
    {
        // Type 1 (ExtraPercentOff) requires the Percent unit.
        var repository = new FakeCoupons();
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest(discountUnit: "Amount")));

        Assert.Equal("COUPON_DISCOUNT_INVALID", problem.ErrorCode);
    }

    [Fact]
    public async Task Create_coupon_buy_x_get_y_free_forces_full_discount_percent()
    {
        var repository = new FakeCoupons();
        var service = NewService(repository);

        var result = await service.CreateAsync(4, CreateRequest(
            couponTypeId: CouponTypeIds.BuyXGetYFree,
            condition: new CouponConditionRequest(BuyQuantity: 1, GetQuantity: 1, null, null, null, null)));

        Assert.NotNull(result.Condition);
        Assert.Equal(100m, result.Condition!.GetDiscountPercent);
    }

    [Fact]
    public async Task Create_coupon_requires_condition_when_type_needs_one()
    {
        var repository = new FakeCoupons();
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.CreateAsync(4, CreateRequest(couponTypeId: CouponTypeIds.BuyXGetYFree)));

        Assert.Equal("COUPON_DISCOUNT_INVALID", problem.ErrorCode);
    }

    [Fact]
    public async Task Validate_returns_out_of_date_range_after_end_date()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(2));
        var repository = new FakeCoupons(coupon);
        var service = NewService(repository);

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId, null);

        Assert.False(result.IsValid);
        Assert.Equal("OUT_OF_DATE_RANGE", result.Reason);
    }

    [Fact]
    public async Task Validate_returns_usage_limit_reached_when_exhausted()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.UsageLimit = 2;
        coupon.UsedCount = 2;
        var repository = new FakeCoupons(coupon);
        var service = NewService(repository);

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId, null);

        Assert.False(result.IsValid);
        Assert.Equal("USAGE_LIMIT_REACHED", result.Reason);
    }

    [Fact]
    public async Task Validate_returns_valid_when_within_limits()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.UsageLimit = 5;
        coupon.UsedCount = 2;
        var repository = new FakeCoupons(coupon);
        var service = NewService(repository);

        var result = await service.ValidateAsync(coupon.Code, coupon.ProductId, null);

        Assert.True(result.IsValid);
        Assert.Equal(coupon.DiscountValue, result.DiscountValue);
    }

    [Fact]
    public async Task Delete_rejects_coupon_already_used()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        coupon.UsedCount = 1;
        var repository = new FakeCoupons(coupon);
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() => service.DeleteAsync(4, coupon.Id));

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("COUPON_ALREADY_USED", problem.ErrorCode);
        Assert.False(repository.WasRemoved);
    }

    [Fact]
    public async Task Delete_rejects_when_not_owner()
    {
        var coupon = SampleCoupon(start: Utc(1), end: Utc(20));
        var repository = new FakeCoupons(coupon);
        var service = NewService(repository);

        var problem = await Assert.ThrowsAsync<AppProblemException>(() => service.DeleteAsync(999, coupon.Id));

        Assert.Equal(403, problem.StatusCode);
        Assert.Equal("FORBIDDEN_NOT_OWNER", problem.ErrorCode);
    }

    private static CouponService NewService(FakeCoupons repository, bool recaptchaSuccess = true) =>
        new(repository, new FakeCouponTypes(), new FakeUnitOfWork(), new FixedClock(), new FakeRecaptchaVerifier(recaptchaSuccess));

    private static CreateCouponRequest CreateRequest(
        string code = "SUMMER10",
        decimal discountValue = 10,
        string discountUnit = "Percent",
        int couponTypeId = CouponTypeIds.ExtraPercentOff,
        CouponConditionRequest? condition = null) =>
        new(
            CouponTypeId: couponTypeId,
            Name: "Sample coupon",
            Code: code,
            DiscountValue: discountValue,
            DiscountUnit: discountUnit,
            MaxDiscount: null,
            StartDate: Utc(1),
            EndDate: Utc(20),
            UsageLimit: 5,
            UsagePerUser: null,
            MinimumOrderValue: null,
            ApplicablePriceMin: null,
            ApplicablePriceMax: null,
            CategoryId: null,
            ProductId: 10,
            IsActive: true,
            Condition: condition,
            RecaptchaToken: "token");

    private static Coupon SampleCoupon(DateTime start, DateTime end) => new()
    {
        Id = 1,
        SellerId = 4,
        ProductId = 10,
        Name = "Sample coupon",
        Code = "SUMMER10",
        CouponTypeId = CouponTypeIds.ExtraPercentOff,
        DiscountValue = 10,
        DiscountUnit = CouponDiscountUnit.Percent,
        StartDate = start,
        EndDate = end,
        UsedCount = 0,
        IsActive = true,
        CreatedAtUtc = start,
        UpdatedAtUtc = start
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

    private sealed class FakeCouponTypes : ICouponTypeRepository
    {
        public Task<CouponType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CouponType?>(new CouponType { Id = id, Name = $"Type {id}", IsActive = true });

        public Task<IReadOnlyList<CouponType>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CouponType>>([]);
    }

    private sealed class FakeCoupons(Coupon? coupon = null) : ICouponRepository
    {
        public List<Coupon> Added { get; } = [];
        public bool WasRemoved { get; private set; }
        public bool CodeAlreadyExists { get; init; }

        public Task<bool> CodeExistsAsync(string code, int? excludeCouponId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CodeAlreadyExists);

        public Task<Coupon?> GetByIdAsync(int id, bool tracked, CancellationToken cancellationToken = default) =>
            Task.FromResult(coupon?.Id == id ? coupon : null);

        public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(coupon is not null && coupon.Code == code ? coupon : null);

        public Task<IReadOnlyList<Coupon>> ListForSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Coupon>>(coupon is not null && coupon.SellerId == sellerId ? [coupon] : []);

        public Task AddAsync(Coupon value, CancellationToken cancellationToken = default)
        {
            value.Id = Added.Count + 1;
            Added.Add(value);
            coupon = value;
            return Task.CompletedTask;
        }

        public void Remove(Coupon value) => WasRemoved = true;
    }
}

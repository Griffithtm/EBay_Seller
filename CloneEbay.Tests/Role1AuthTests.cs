using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Auth;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class Role1AuthTests
{
    [Fact]
    public async Task Register_normalizes_email_hashes_password_and_creates_buyer()
    {
        var users = new FakeUsers();
        var service = CreateService(users);

        var result = await service.RegisterAsync(new RegisterRequest("new.seller", "SELLER@Example.COM", "StrongPass1"));

        Assert.Equal("seller@example.com", result.Email);
        Assert.Equal("Buyer", result.Role);
        Assert.Single(users.Items);
        Assert.Equal("HASH:StrongPass1", users.Items[0].PasswordHash);
    }

    [Fact]
    public async Task Login_sixth_failed_attempt_is_rate_limited_for_fifteen_minutes()
    {
        var users = new FakeUsers();
        users.Items.Add(new User
        {
            Id = 7,
            Username = "buyer",
            Email = "buyer@example.com",
            PasswordHash = "HASH:CorrectPass1",
            Role = UserRole.Buyer,
            IsActive = true
        });
        var service = CreateService(users);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failure = await Assert.ThrowsAsync<AppProblemException>(() =>
                service.LoginAsync(new LoginRequest("buyer@example.com", "WrongPass1")));
            Assert.Equal(401, failure.StatusCode);
        }

        var locked = await Assert.ThrowsAsync<AppProblemException>(() =>
            service.LoginAsync(new LoginRequest("buyer@example.com", "CorrectPass1")));
        Assert.Equal(429, locked.StatusCode);
        Assert.Equal("ACCOUNT_TEMPORARILY_LOCKED", locked.ErrorCode);
    }

    private static AuthService CreateService(FakeUsers users) => new(
        users,
        new FakeRefreshTokens(),
        new FakeUnitOfWork(),
        new FakePasswordHasher(),
        new FakeTokenService(),
        new FixedClock());

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> Items { get; } = [];
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(user => user.Id == id));
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(user => user.Email == normalizedEmail));
        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.Id = Items.Count + 1;
            Items.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokens : IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);
        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"HASH:{password}";
        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessTokenResult CreateAccessToken(User user, Store? store) =>
            new("access", new DateTime(2026, 8, 23, 13, 0, 0, DateTimeKind.Utc));
        public RefreshTokenResult CreateRefreshToken() =>
            new("refresh", "hash", new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc));
        public string HashRefreshToken(string token) => $"hash:{token}";
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
    }
}

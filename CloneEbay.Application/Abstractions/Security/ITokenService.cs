using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Security;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
public sealed record RefreshTokenResult(string Token, string TokenHash, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, Store? store);
    RefreshTokenResult CreateRefreshToken();
    string HashRefreshToken(string token);
}

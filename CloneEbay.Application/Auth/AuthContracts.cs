using System.Text.Json.Serialization;

namespace CloneEbay.Application.Auth;

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshAccessTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record UserSessionResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    int? StoreId,
    [property: JsonPropertyName("verified")] bool SellerVerified);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    UserSessionResponse User);

public sealed record RegisterResponse(int Id, string Username, string Email, string Role);

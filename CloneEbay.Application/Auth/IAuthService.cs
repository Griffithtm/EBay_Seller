namespace CloneEbay.Application.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokensResponse> RefreshAsync(RefreshAccessTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}

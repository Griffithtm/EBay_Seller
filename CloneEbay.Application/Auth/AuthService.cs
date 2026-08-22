using System.Net;
using System.Net.Mail;
using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = NormalizeEmail(request.Email);

        if (username.Length is < 3 or > 100)
        {
            throw Problem(422, "USERNAME_INVALID", "Username must contain between 3 and 100 characters.");
        }

        ValidatePassword(request.Password);

        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw Problem(409, "EMAIL_ALREADY_EXISTS", "This email address is already registered.");
        }

        var now = clock.UtcNow;
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Buyer,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(user.Id, user.Username, user.Email, user.Role.ToString());
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            throw Problem(401, "INVALID_CREDENTIALS", "Email or password is incorrect.");
        }

        var now = clock.UtcNow;
        if (!user.IsActive)
        {
            throw Problem(403, "ACCOUNT_DISABLED", "This account has been disabled.");
        }

        if (user.LockoutEndUtc > now)
        {
            throw Problem(429, "ACCOUNT_TEMPORARILY_LOCKED", "Too many failed attempts. Please try again later.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            user.UpdatedAtUtc = now;
            if (user.FailedLoginAttempts >= 5)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = now.AddMinutes(15);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw Problem(401, "INVALID_CREDENTIALS", "Email or password is incorrect.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = now;
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshAccessTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw Problem(401, "REFRESH_TOKEN_INVALID", "Refresh token is missing or invalid.");
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var currentToken = await refreshTokens.GetWithUserByHashAsync(tokenHash, cancellationToken);
        var now = clock.UtcNow;

        if (currentToken is null || !currentToken.IsActive(now) || !currentToken.User.IsActive)
        {
            throw Problem(401, "REFRESH_TOKEN_INVALID", "Refresh token is expired, revoked, or invalid.");
        }

        var nextRefreshToken = tokenService.CreateRefreshToken();
        currentToken.RevokedAtUtc = now;
        currentToken.ReplacedByTokenHash = nextRefreshToken.TokenHash;

        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId = currentToken.UserId,
            TokenHash = nextRefreshToken.TokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = nextRefreshToken.ExpiresAtUtc
        }, cancellationToken);

        var accessToken = tokenService.CreateAccessToken(currentToken.User, currentToken.User.Store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapTokens(currentToken.User, accessToken, nextRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var token = await refreshTokens.GetWithUserByHashAsync(tokenHash, cancellationToken);
        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user, user.Store);
        var refreshToken = tokenService.CreateRefreshToken();

        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            CreatedAtUtc = clock.UtcNow,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTokens(user, accessToken, refreshToken);
    }

    private static AuthTokensResponse MapTokens(
        User user,
        AccessTokenResult accessToken,
        RefreshTokenResult refreshToken)
    {
        var verified = user.Store?.VerificationStatus == VerificationStatus.Approved;
        return new AuthTokensResponse(
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.ExpiresAtUtc,
            new UserSessionResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role.ToString(),
                user.Store?.Id,
                verified));
    }

    private static string NormalizeEmail(string email)
    {
        try
        {
            return new MailAddress(email.Trim()).Address.ToLowerInvariant();
        }
        catch
        {
            throw Problem(422, "EMAIL_INVALID", "A valid email address is required.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw Problem(422, "PASSWORD_WEAK", "Password must have at least 8 characters, including upper-case, lower-case, and a number.");
        }
    }

    private static AppProblemException Problem(int statusCode, string code, string message) =>
        new(statusCode, code, message);
}

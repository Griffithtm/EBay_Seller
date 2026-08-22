using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Globalization;
using CloneEbay.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CloneEbay.Web.Services;

public sealed class SellerApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SellerApiClient> logger)
{
    private const string AccessTokenCookie = "cloneebay.api.access";
    private const string RefreshTokenCookie = "cloneebay.api.refresh";
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiCallResult<T>> GetAsync<T>(string path, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, authenticated, true, cancellationToken);

    public Task<ApiCallResult<T>> PostAsync<T>(string path, object body, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, () => JsonContent(body), authenticated, true, cancellationToken);

    public Task<ApiCallResult<T>> PutAsync<T>(string path, object? body, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, path, body is null ? null : () => JsonContent(body), authenticated, true, cancellationToken);

    public Task<ApiCallResult<T>> PatchAsync<T>(string path, object? body = null, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Patch, path, body is null ? null : () => JsonContent(body), authenticated, true, cancellationToken);

    public Task<ApiCallResult<T>> DeleteAsync<T>(string path, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, authenticated, true, cancellationToken);

    public Task<ApiCallResult<StoreProfileDto>> UpdateStoreAsync(
        StoreProfileViewModel model,
        CancellationToken cancellationToken = default) =>
        SendAsync<StoreProfileDto>(HttpMethod.Put, "api/store", () => CreateStoreContent(model), true, true, cancellationToken);

    public Task<ApiCallResult<ProductDto>> CreateProductAsync(
        ProductFormViewModel model,
        CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, "api/products", () => CreateProductContent(model, includeImages: true), true, true, cancellationToken);

    public Task<ApiCallResult<ProductDto>> UpdateProductAsync(
        ProductFormViewModel model,
        CancellationToken cancellationToken = default) =>
        SendAsync<ProductDto>(HttpMethod.Put, $"api/products/{model.Id}", () => CreateProductContent(model, includeImages: model.Images.Count > 0), true, true, cancellationToken);

    public async Task EstablishSessionAsync(AuthTokensDto tokens)
    {
        var context = GetHttpContext();
        var secure = context.Request.IsHttps;
        context.Response.Cookies.Append(AccessTokenCookie, tokens.AccessToken, CookieOptions(tokens.AccessTokenExpiresAtUtc, secure));
        context.Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, CookieOptions(tokens.RefreshTokenExpiresAtUtc, secure));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tokens.User.Id.ToString()),
            new(ClaimTypes.Name, tokens.User.Username),
            new(ClaimTypes.Email, tokens.User.Email),
            new(ClaimTypes.Role, tokens.User.Role),
            new("verified", tokens.User.SellerVerified.ToString().ToLowerInvariant())
        };
        if (tokens.User.StoreId.HasValue)
        {
            claims.Add(new Claim("storeId", tokens.User.StoreId.Value.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = tokens.RefreshTokenExpiresAtUtc });
    }

    public Task<bool> RefreshSessionAsync(CancellationToken cancellationToken = default) =>
        TryRefreshAsync(cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var context = GetHttpContext();
        var refreshToken = context.Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await SendAsync<object>(
                HttpMethod.Post,
                "api/auth/logout",
                () => JsonContent(new { refreshToken }),
                true,
                false,
                cancellationToken);
        }

        context.Response.Cookies.Delete(AccessTokenCookie);
        context.Response.Cookies.Delete(RefreshTokenCookie);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task<ApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        Func<HttpContent?>? contentFactory,
        bool authenticated,
        bool allowRefresh,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (contentFactory is not null)
        {
            request.Content = contentFactory();
        }

        if (authenticated)
        {
            var accessToken = GetHttpContext().Request.Cookies[AccessTokenCookie];
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated && allowRefresh && await TryRefreshAsync(cancellationToken))
        {
            return await SendAsync<T>(method, path, contentFactory, true, false, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new ApiCallResult<T>(true, default, null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        ApiEnvelopeDto<T>? envelope = null;
        try
        {
            envelope = JsonSerializer.Deserialize<ApiEnvelopeDto<T>>(json, _jsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Could not parse API response from {Path}.", path);
        }

        return new ApiCallResult<T>(
            response.IsSuccessStatusCode && envelope?.Success == true,
            envelope is null ? default : envelope.Data,
            envelope?.Error ?? (response.IsSuccessStatusCode ? null : new ApiErrorDto("API_ERROR", "The API request failed.", null)),
            response.StatusCode);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var context = GetHttpContext();
        var refreshToken = context.Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var result = await SendAsync<AuthTokensDto>(
            HttpMethod.Post,
            "api/auth/refresh-token",
            () => JsonContent(new { refreshToken }),
            false,
            false,
            cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return false;
        }

        await EstablishSessionAsync(result.Data);
        return true;
    }

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");

    private static MultipartFormDataContent CreateStoreContent(StoreProfileViewModel model)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.StoreName), "StoreName" },
            { new StringContent(model.Description ?? string.Empty), "Description" }
        };
        if (model.Banner is not null)
        {
            var fileContent = new StreamContent(model.Banner.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.Banner.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "Banner", model.Banner.FileName);
        }
        return content;
    }

    private static MultipartFormDataContent CreateProductContent(ProductFormViewModel model, bool includeImages)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.Title), "Title" },
            { new StringContent(model.Description), "Description" },
            { new StringContent(model.Price.ToString(CultureInfo.InvariantCulture)), "Price" },
            { new StringContent(model.CategoryId.ToString(CultureInfo.InvariantCulture)), "CategoryId" },
            { new StringContent(model.IsAuction.ToString().ToLowerInvariant()), "IsAuction" },
            { new StringContent(model.Quantity.ToString(CultureInfo.InvariantCulture)), "Quantity" }
        };
        if (model.AuctionEndTime.HasValue)
        {
            content.Add(new StringContent(model.AuctionEndTime.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)), "AuctionEndTime");
        }
        if (includeImages)
        {
            foreach (var image in model.Images)
            {
                var fileContent = new StreamContent(image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "Images", image.FileName);
            }
        }
        return content;
    }

    private static CookieOptions CookieOptions(DateTime expiresAtUtc, bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Expires = new DateTimeOffset(expiresAtUtc)
    };

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");
}

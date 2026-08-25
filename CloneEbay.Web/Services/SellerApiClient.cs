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
        SendAsync<ProductDto>(HttpMethod.Put, $"api/products/{model.Id}", () => CreateProductContent(model, includeImages: model.Images is { Count: > 0 }), true, true, cancellationToken);

    public Task<ApiCallResult<PagedResultDto<ProductDto>>> GetProductsAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<ProductDto>>($"api/products?page={page}&pageSize={pageSize}", cancellationToken: cancellationToken);

    public Task<ApiCallResult<OrderDetailDto>> CreateManualOrderAsync(
        CreateManualOrderViewModel model,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            buyerFullName = model.BuyerFullName,
            buyerPhone = model.BuyerPhone,
            country = model.Country,
            city = model.City,
            district = model.District,
            street = model.Street,
            zipCode = model.ZipCode,
            paymentMethod = model.PaymentMethod,
            items = new[]
            {
                new
                {
                    productId = model.ProductId,
                    quantity = model.Quantity,
                    unitPrice = (decimal?)null
                }
            }
        };

        return PostAsync<OrderDetailDto>("api/orders", body, authenticated: true, cancellationToken: cancellationToken);
    }

    public Task<ApiCallResult<PayPalCheckoutResponseDto>> CreatePayPalCheckoutAsync(
        int orderId,
        string? returnUrl = null,
        string? cancelUrl = null,
        CancellationToken cancellationToken = default) =>
        PostAsync<PayPalCheckoutResponseDto>($"api/orders/{orderId}/paypal-checkout", new { returnUrl, cancelUrl }, authenticated: true, cancellationToken: cancellationToken);

    public Task<ApiCallResult<OrderDetailDto>> CapturePayPalCheckoutAsync(
        int orderId,
        string paypalOrderId,
        CancellationToken cancellationToken = default) =>
        PostAsync<OrderDetailDto>($"api/orders/{orderId}/paypal-capture", new { paypalOrderId }, authenticated: true, cancellationToken: cancellationToken);

    public Task<ApiCallResult<VNPayCheckoutResponseDto>> CreateVNPayCheckoutAsync(
        int orderId,
        string? returnUrl = null,
        CancellationToken cancellationToken = default) =>
        PostAsync<VNPayCheckoutResponseDto>($"api/orders/{orderId}/vnpay-checkout", new { returnUrl }, authenticated: true, cancellationToken: cancellationToken);

    public Task<ApiCallResult<OrderDetailDto>> CaptureVNPayCheckoutAsync(
        int orderId,
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken = default) =>
        PostAsync<OrderDetailDto>($"api/orders/{orderId}/vnpay-capture", new { queryParams }, authenticated: true, cancellationToken: cancellationToken);

    public Task<ApiCallResult<OrderDetailDto>> SimulateDeliveryAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        PostAsync<OrderDetailDto>($"api/orders/{orderId}/simulate-delivery", new { }, authenticated: true, cancellationToken: cancellationToken);

    public Task<ApiCallResult<CouponDto>> CreateCouponAsync(
        CouponFormViewModel model,
        CancellationToken cancellationToken = default) =>
        PostAsync<CouponDto>("api/coupons", new
        {
            productId = model.ProductId,
            code = model.Code,
            discountPercent = model.DiscountPercent,
            startDate = model.StartDate.ToUniversalTime(),
            endDate = model.EndDate.ToUniversalTime(),
            maxUsage = model.MaxUsage,
            recaptchaToken = model.RecaptchaToken
        }, cancellationToken: cancellationToken);

    public Task<ApiCallResult<CouponDto>> UpdateCouponAsync(
        int id,
        CouponFormViewModel model,
        CancellationToken cancellationToken = default) =>
        PutAsync<CouponDto>($"api/coupons/{id}", new
        {
            code = model.Code,
            discountPercent = model.DiscountPercent,
            startDate = model.StartDate.ToUniversalTime(),
            endDate = model.EndDate.ToUniversalTime(),
            maxUsage = model.MaxUsage
        }, cancellationToken: cancellationToken);

    public Task<ApiCallResult<ConversationListResponse>> GetConversationsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ConversationListResponse>("api/messages/conversations", true, cancellationToken);

    public Task<ApiCallResult<PagedResultDto<MessageResponse>>> GetMessagesAsync(int conversationWith, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<MessageResponse>>($"api/messages/{conversationWith}?page={page}&pageSize={pageSize}", true, cancellationToken);

    public Task<ApiCallResult<MessageResponse>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<MessageResponse>("api/messages", request, true, cancellationToken);

    public Task<ApiCallResult<object>> MarkAsReadAsync(int messageId, CancellationToken cancellationToken = default) =>
        PutAsync<object>($"api/messages/{messageId}/read", null, true, cancellationToken);

    public Task<ApiCallResult<object>> MarkConversationAsReadAsync(int otherUserId, CancellationToken cancellationToken = default) =>
        PutAsync<object>($"api/messages/conversations/{otherUserId}/read", null, true, cancellationToken);

    public Task<ApiCallResult<UnreadCountResponse>> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        GetAsync<UnreadCountResponse>("api/messages/unread-count", true, cancellationToken);


    public Task<ApiCallResult<PagedResultDto<OrderSummaryDto>>> GetOrdersAsync(
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        GetAsync<PagedResultDto<OrderSummaryDto>>($"api/orders?status={Uri.EscapeDataString(status ?? "")}&page={page}&pageSize={pageSize}", cancellationToken: cancellationToken);

    public Task<ApiCallResult<OrderDetailDto>> GetOrderDetailAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        GetAsync<OrderDetailDto>($"api/orders/{id}", cancellationToken: cancellationToken);


    public Task<ApiCallResult<ShippingInfoDto>> GetShippingInfoAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        GetAsync<ShippingInfoDto>($"api/shipping/{orderId}", cancellationToken: cancellationToken);

    public Task<ApiCallResult<PaymentDetailDto>> GetPaymentInfoAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        GetAsync<PaymentDetailDto>($"api/orders/{orderId}/payment", cancellationToken: cancellationToken);


    public Task<ApiCallResult<OrderConfirmDto>> CancelOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        PostAsync<OrderConfirmDto>($"api/orders/{orderId}/cancel", new { }, cancellationToken: cancellationToken);


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
        if (includeImages && model.Images is not null)
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

    public Task<ApiCallResult<PagedResultDto<ReviewResponseDto>>> GetReviewsAsync(int? productId, int? sellerId, bool? hasReply, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, int page = 1, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<ReviewResponseDto>>($"api/reviews?productId={productId}&sellerId={sellerId}&hasReply={hasReply}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&searchTerm={Uri.EscapeDataString(searchTerm ?? string.Empty)}&page={page}", true, ct);

    public Task<ApiCallResult<ReviewResponseDto>> ReplyToReviewAsync(int reviewId, string response, CancellationToken ct = default) =>
        PostAsync<ReviewResponseDto>($"api/reviews/{reviewId}/reply", new { Response = response }, true, ct);

    public Task<ApiCallResult<FeedbackResponseDto>> GetFeedbackAsync(int sellerId, CancellationToken ct = default) =>
        GetAsync<FeedbackResponseDto>($"api/feedback/{sellerId}", false, ct);

    public Task<ApiCallResult<PagedResultDto<ReturnRequestResponseDto>>> GetReturnsAsync(string? status, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, int page = 1, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<ReturnRequestResponseDto>>($"api/returns?status={status}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&searchTerm={Uri.EscapeDataString(searchTerm ?? string.Empty)}&page={page}", true, ct);

    public Task<ApiCallResult<ReturnRequestResponseDto>> UpdateReturnStatusAsync(int returnId, string status, CancellationToken ct = default) =>
        PutAsync<ReturnRequestResponseDto>($"api/returns/{returnId}/status", new { Status = status }, true, ct);

    public Task<ApiCallResult<PagedResultDto<DisputeResponseDto>>> GetDisputesAsync(string? status, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, int page = 1, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<DisputeResponseDto>>($"api/disputes?status={status}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&searchTerm={Uri.EscapeDataString(searchTerm ?? string.Empty)}&page={page}", true, ct);

    public Task<ApiCallResult<DisputeResponseDto>> ResolveDisputeAsync(int disputeId, string resolution, CancellationToken ct = default) =>
        PutAsync<DisputeResponseDto>($"api/disputes/{disputeId}/resolve", new { Resolution = resolution }, true, ct);

    public Task<ApiCallResult<BuyerSummaryResponseDto>> GetBuyerSummaryAsync(int buyerId, CancellationToken ct = default) =>
        GetAsync<BuyerSummaryResponseDto>($"api/seller/buyer-summary/{buyerId}", true, ct);

    public Task<ApiCallResult<Dictionary<string, string>>> UploadMessageAttachmentAsync(IFormFile file, CancellationToken ct = default)
    {
        return SendAsync<Dictionary<string, string>>(
            HttpMethod.Post,
            "api/messages/upload-attachment",
            () =>
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "file", file.FileName);
                return content;
            },
            true,
            true,
            ct);
    }

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");
}





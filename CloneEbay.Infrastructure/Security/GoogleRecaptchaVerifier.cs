using System.Text.Json;
using System.Text.Json.Serialization;
using CloneEbay.Application.Abstractions.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Security;

/// <summary>
/// Verifies Google reCAPTCHA v3 tokens against the siteverify endpoint.
/// Scores below the configured minimum, action mismatches, and transport
/// failures are all logged so repeated abuse attempts stay traceable.
/// </summary>
public sealed class GoogleRecaptchaVerifier(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GoogleRecaptchaVerifier> logger) : IRecaptchaVerifier
{
    public async Task<RecaptchaVerificationResult> VerifyAsync(
        string? token,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Recaptcha:Enabled", false))
        {
            // Verification is opt-in so local/dev environments without a Google site key still work.
            return new RecaptchaVerificationResult(true, 1.0, null);
        }

        var secretKey = configuration["Recaptcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            logger.LogWarning("reCAPTCHA is enabled but Recaptcha:SecretKey is not configured; rejecting action {Action}.", action);
            return new RecaptchaVerificationResult(false, 0, "MISSING_SECRET");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("reCAPTCHA verification failed for action {Action}: token was not supplied.", action);
            return new RecaptchaVerificationResult(false, 0, "MISSING_TOKEN");
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                }),
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "reCAPTCHA siteverify request failed for action {Action}.", action);
            return new RecaptchaVerificationResult(false, 0, "SITEVERIFY_UNAVAILABLE");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("reCAPTCHA siteverify call returned {Status} for action {Action}.", response.StatusCode, action);
            return new RecaptchaVerificationResult(false, 0, "SITEVERIFY_HTTP_ERROR");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<RecaptchaSiteVerifyResponse>(responseStream, cancellationToken: cancellationToken);
        if (payload is null || !payload.Success)
        {
            var errors = payload?.ErrorCodes is { Length: > 0 } codes ? string.Join(",", codes) : "unknown";
            logger.LogWarning("reCAPTCHA rejected token for action {Action}. Errors: {Errors}.", action, errors);
            return new RecaptchaVerificationResult(false, payload?.Score ?? 0, errors);
        }

        if (!string.Equals(payload.Action, action, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "reCAPTCHA action mismatch: expected {Expected}, received {Actual}.",
                action,
                payload.Action);
            return new RecaptchaVerificationResult(false, payload.Score, "ACTION_MISMATCH");
        }

        var minimumScore = configuration.GetValue("Recaptcha:MinimumScore", 0.5);
        if (payload.Score < minimumScore)
        {
            logger.LogWarning(
                "reCAPTCHA score {Score} is below the minimum {Minimum} for action {Action}.",
                payload.Score,
                minimumScore,
                action);
            return new RecaptchaVerificationResult(false, payload.Score, "LOW_SCORE");
        }

        return new RecaptchaVerificationResult(true, payload.Score, null);
    }

    private sealed class RecaptchaSiteVerifyResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("score")] public double Score { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("challenge_ts")] public string? ChallengeTimestamp { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
    }
}

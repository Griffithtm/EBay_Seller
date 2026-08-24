namespace CloneEbay.Application.Abstractions.Security;

public sealed record RecaptchaVerificationResult(bool Success, double Score, string? ErrorCode);

public interface IRecaptchaVerifier
{
    Task<RecaptchaVerificationResult> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default);
}

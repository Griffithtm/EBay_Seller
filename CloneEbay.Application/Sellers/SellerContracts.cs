using CloneEbay.Application.Common;
using System.Text.Json.Serialization;

namespace CloneEbay.Application.Sellers;

public sealed record SellerApplyRequest(
    string SellerType,
    string LegalName,
    string Phone,
    string StoreName);

public sealed record RejectSellerRequest(string Reason);

public sealed record SellerVerificationResponse(
    bool HasApplication,
    int? StoreId,
    string? StoreName,
    [property: JsonPropertyName("verificationStatus")] string? Status,
    string? RejectionReason,
    DateTime? SubmittedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record SellerApplicationResponse(
    int StoreId,
    int SellerId,
    string SellerEmail,
    string SellerUsername,
    string StoreName,
    string SellerType,
    string LegalName,
    string ContactPhone,
    [property: JsonPropertyName("verificationStatus")] string Status,
    string? RejectionReason,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);

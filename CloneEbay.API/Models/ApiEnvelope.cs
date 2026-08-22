namespace CloneEbay.API.Models;

public sealed record ApiError(string Code, string Message, object? Details = null);
public sealed record ApiMeta(string TraceId);

public sealed record ApiEnvelope<T>(bool Success, T? Data, ApiError? Error, ApiMeta Meta)
{
    public static ApiEnvelope<T> Ok(T data, string traceId) => new(true, data, null, new ApiMeta(traceId));

    public static ApiEnvelope<T> Failure(string code, string message, string traceId, object? details = null) =>
        new(false, default, new ApiError(code, message, details), new ApiMeta(traceId));
}

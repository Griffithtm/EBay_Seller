using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CloneEbay.API.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppProblemException exception)
        {
            await WriteAsync(context, exception.StatusCode, exception.ErrorCode, exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database constraint violation. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteAsync(context, StatusCodes.Status409Conflict, "DATABASE_CONFLICT", "The requested change conflicts with existing data.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Malformed JSON request. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteAsync(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "The JSON request body is invalid.");
        }
        catch (BadHttpRequestException exception)
        {
            logger.LogWarning(exception, "Invalid HTTP request. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteAsync(
                context,
                exception.StatusCode,
                exception.StatusCode == StatusCodes.Status413PayloadTooLarge ? "PAYLOAD_TOO_LARGE" : "VALIDATION_ERROR",
                exception.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "The request body is too large."
                    : "The HTTP request is invalid.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiEnvelope<object>.Failure(code, message, context.TraceIdentifier));
    }
}

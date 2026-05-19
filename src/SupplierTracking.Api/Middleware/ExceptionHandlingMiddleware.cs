using FluentValidation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupplierTracking.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        int statusCode;
        string message;
        Dictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException ve:
                statusCode = StatusCodes.Status400BadRequest;
                message    = "One or more validation errors occurred.";
                errors     = ve.Errors
                    .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                message    = exception.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                message    = exception.Message;
                break;

            case InvalidOperationException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                message    = exception.Message;
                break;

            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                message    = exception.Message;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message    = "An unexpected error occurred.";
                break;
        }

        if (statusCode >= 500)
            _logger.LogError(exception,
                "Unhandled server error [{TraceId}] on {Method} {Path}",
                traceId, context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(
                "Client error {StatusCode} [{TraceId}] on {Method} {Path}: {Message}",
                statusCode, traceId, context.Request.Method, context.Request.Path, exception.Message);

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var body = new ErrorResponse(statusCode, message, traceId, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}

/// <summary>Standard error envelope returned for all non-2xx responses.</summary>
/// <param name="Status">HTTP status code.</param>
/// <param name="Message">Human-readable description of the error.</param>
/// <param name="TraceId">Correlation ID — include this when reporting bugs.</param>
/// <param name="Errors">Field-level validation errors (only present on 400 validation failures).</param>
internal sealed record ErrorResponse(
    int                               Status,
    string                            Message,
    string                            TraceId,
    Dictionary<string, string[]>?     Errors);

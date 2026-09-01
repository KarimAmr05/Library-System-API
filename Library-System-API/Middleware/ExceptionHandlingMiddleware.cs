using LibrarySystem.Shared.Constants;
using LibrarySystem.API.Extensions;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.API.Middleware;

/// <summary>
/// Centralized handler for unexpected exceptions. Logs the full exception
/// server-side and returns a sanitized error envelope (no stack traces) with a
/// trace identifier for correlation.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">Structured logger.</param>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<ExceptionHandlingMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Invokes the middleware, converting unhandled exceptions into HTTP 500 responses.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ErrorResponse
            {
                Code = ErrorCodes.InternalError,
                Message = "An unexpected error occurred while processing the request.",
                Details = Array.Empty<ErrorDetail>(),
                TraceId = traceId
            };

            await context.Response.WriteAsJsonAsync(payload, context.RequestAborted).ConfigureAwait(false);
        }
    }
}

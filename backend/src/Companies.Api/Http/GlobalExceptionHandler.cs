using Microsoft.AspNetCore.Diagnostics;

namespace Companies.Api.Http;

/// <summary>
/// Last line of defence: logs any unhandled exception and returns a generic 500 problem that
/// leaks nothing about the failure. A request the client aborted is not an error.
/// </summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.RequestAborted(httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        logger.UnhandledException(exception, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The server could not complete the request. Please try again later.",
            },
        }).ConfigureAwait(false);
    }
}

internal static partial class GlobalExceptionHandlerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {Method} {Path} was aborted by the client")]
    public static partial void RequestAborted(this ILogger logger, string method, PathString path);
}

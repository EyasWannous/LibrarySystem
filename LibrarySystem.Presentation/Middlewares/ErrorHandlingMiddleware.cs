using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace LibrarySystem.Presentation.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occured: {Message}", exception.Message);

        switch (exception)
        {
            case DbException:
                _logger.LogError(exception, "Database connection error occurred");
                await HandleDatabaseExceptionAsync(httpContext, cancellationToken);
                break;
            case HttpRequestException:
                _logger.LogError(exception, "Network connection error occurred");
                await HandleConnectionExceptionAsync(httpContext, cancellationToken);
                break;
            case Exception:
                _logger.LogError(exception, "An unhandled exception occurred");
                await HandleExceptionAsync(httpContext, cancellationToken);
                break;
        }

        return true;
    }

    private static async Task HandleConnectionExceptionAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Service unavailable. Please check your network connection.",
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/503",
        };

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
    }

    private static async Task HandleDatabaseExceptionAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Database connection error. Please try again later.",
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/500",
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
    }

    private static async Task HandleExceptionAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/500",
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
    }
}

using Microsoft.AspNetCore.Mvc;

namespace TeamHub.Server.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into the same
/// ProblemDetails shape endpoints already return for expected failures
/// (see Domain/Common/Result+Error and e.g. AuthEndpoints' Results.Problem
/// calls), instead of leaking a raw framework error page/stack trace.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "InternalServerError",
                // Only expose exception details in Development — production
                // callers get a generic message, matching the JWT-secret
                // "don't leak secrets/internals" posture elsewhere in this repo.
                Detail = _environment.IsDevelopment()
                    ? ex.ToString()
                    : "An unexpected error occurred."
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

using Serilog;
using System.Diagnostics;
using System.Text;

namespace BeQuestionBank.API.Middlewares;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        // Bỏ qua Swagger, favicon, healthcheck
        if (path.StartsWith("/swagger") || path.StartsWith("/favicon") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Log Request
        _logger.LogInformation("➡️ {Method} {Path}", context.Request.Method, context.Request.Path);

        await _next(context);

        stopwatch.Stop();

        // Log Response
        _logger.LogInformation("⬅️ {StatusCode} {Path} ({Elapsed} ms)",
            context.Response.StatusCode,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
    }
}

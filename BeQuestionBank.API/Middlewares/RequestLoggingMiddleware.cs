using Serilog;
using System.Diagnostics;
using System.Text;

namespace BeQuestionBank.API.Middlewares;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // 📌 Lấy thông tin request
        var request = context.Request;
        request.EnableBuffering(); // Cho phép đọc body nhiều lần

        string bodyAsText = "";
        if (request.ContentLength > 0 && request.Body.CanSeek)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            bodyAsText = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        Log.Information("➡️ HTTP Request: {Method} {Path} {Query} Body={Body}",
            request.Method,
            request.Path,
            request.QueryString.ToString(),
            bodyAsText);

        // 📌 Bắt Response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        stopwatch.Stop();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        Log.Information("⬅️ HTTP Response: {StatusCode} ({Elapsed} ms) Body={Body}",
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            responseText);

        // Trả response về client
        await responseBody.CopyToAsync(originalBodyStream);
    }
}

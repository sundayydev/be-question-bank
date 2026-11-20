namespace BeQuestionBank.API.Middlewares;

using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Shared.DTOs.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failure occurred: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = ApiResponseFactory.ValidationError<object>(ex.Message, ex.Errors);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            var response = ApiResponseFactory.NotFound<object>(ex.Message);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict error occurred: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            var response = ApiResponseFactory.Error<object>(StatusCodes.Status409Conflict, ex.Message);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception occurred: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = ApiResponseFactory.ValidationError<object>(ex.Message);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var response = ApiResponseFactory.ServerError("Đã xảy ra lỗi trong quá trình xử lý.");
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}

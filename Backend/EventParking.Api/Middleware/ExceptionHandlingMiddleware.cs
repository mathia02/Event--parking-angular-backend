using System.Net;
using System.Text.Json;
using EventParking.Business.Exceptions;

namespace EventParking.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(
                context,
                ex);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode =
            HttpStatusCode.InternalServerError;

        var message =
            "An unexpected server error occurred.";

        switch (exception)
        {
            case NotFoundException:
                statusCode =
                    HttpStatusCode.NotFound;

                message =
                    exception.Message;
                break;

            case ValidationException:
                statusCode =
                    HttpStatusCode.BadRequest;

                message =
                    exception.Message;
                break;

            case ConflictException:
                statusCode =
                    HttpStatusCode.Conflict;

                message =
                    exception.Message;
                break;

            case UnauthorizedException:
                statusCode =
                    HttpStatusCode.Unauthorized;

                message =
                    exception.Message;
                break;

            case UnauthorizedAccessException:
                statusCode =
                    HttpStatusCode.Forbidden;

                message =
                    exception.Message;
                break;

            default:
                _logger.LogError(
                    exception,
                    "Unhandled exception occurred while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;
        }

        context.Response.StatusCode =
            (int)statusCode;

        context.Response.ContentType =
            "application/json";

        var response =
            new
            {
                statusCode =
                    (int)statusCode,

                message,

                traceId =
                    context.TraceIdentifier
            };

        var json =
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                });

        await context.Response
            .WriteAsync(json);
    }
}
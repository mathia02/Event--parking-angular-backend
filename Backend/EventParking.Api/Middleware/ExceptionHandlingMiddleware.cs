using System.Net;
using System.Text.Json;
using EventParking.Business.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
            // -----------------------------------------------------
            // NOT FOUND
            // -----------------------------------------------------

            case NotFoundException:
                statusCode =
                    HttpStatusCode.NotFound;

                message =
                    exception.Message;
                break;

            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            case ValidationException:
                statusCode =
                    HttpStatusCode.BadRequest;

                message =
                    exception.Message;
                break;

            // -----------------------------------------------------
            // BUSINESS CONFLICT
            // -----------------------------------------------------

            case ConflictException:
                statusCode =
                    HttpStatusCode.Conflict;

                message =
                    exception.Message;
                break;

            // -----------------------------------------------------
            // EF CORE CONCURRENCY CONFLICT
            // -----------------------------------------------------

            case DbUpdateConcurrencyException:
                statusCode =
                    HttpStatusCode.Conflict;

                message =
                    "The requested resource was modified by another request. " +
                    "Please refresh and try again.";

                _logger.LogWarning(
                    exception,
                    "Database concurrency conflict occurred while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                break;

            // -----------------------------------------------------
            // SQL UNIQUE CONSTRAINT CONFLICT
            //
            // SQL Server:
            // 2601 = Cannot insert duplicate key row
            // 2627 = Violation of UNIQUE constraint
            // -----------------------------------------------------

            case DbUpdateException dbUpdateException
                when IsUniqueConstraintViolation(
                    dbUpdateException):

                statusCode =
                    HttpStatusCode.Conflict;

                message =
                    "The requested item is no longer available " +
                    "or the same record already exists. " +
                    "Please refresh and try again.";

                _logger.LogWarning(
                    exception,
                    "Database unique constraint conflict occurred while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                break;

            // -----------------------------------------------------
            // AUTHENTICATION
            // -----------------------------------------------------

            case UnauthorizedException:
                statusCode =
                    HttpStatusCode.Unauthorized;

                message =
                    exception.Message;
                break;

            // -----------------------------------------------------
            // AUTHORIZATION
            // -----------------------------------------------------

            case UnauthorizedAccessException:
                statusCode =
                    HttpStatusCode.Forbidden;

                message =
                    exception.Message;
                break;

            // -----------------------------------------------------
            // UNKNOWN ERROR
            // -----------------------------------------------------

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

    // -------------------------------------------------------------
    // CHECK SQL SERVER UNIQUE CONSTRAINT ERRORS
    // -------------------------------------------------------------

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        if (exception.InnerException
            is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Number
            is 2601 or 2627;
    }
}
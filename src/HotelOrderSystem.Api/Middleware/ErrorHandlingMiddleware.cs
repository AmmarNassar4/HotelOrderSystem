using System.Net;
using System.Text.Json;
using HotelOrderSystem.Api.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelOrderSystem.Api.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions sJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict.");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "The record was changed by another user. Please refresh and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "Unique constraint violation.");
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "A record with the same unique value already exists.");
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid request format.");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Invalid request format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Unexpected server error.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // SQL Server: 2627 = unique constraint, 2601 = unique index.
        return ex.InnerException is SqlException sqlEx
            && sqlEx.Errors.Cast<SqlError>().Any(e => e.Number is 2627 or 2601);
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, sJsonOptions));
    }
}

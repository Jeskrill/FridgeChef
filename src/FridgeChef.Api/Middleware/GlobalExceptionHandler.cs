using FridgeChef.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace FridgeChef.Api.Middleware;

/// <summary>
/// Global exception handler returning ProblemDetails (RFC 7807).
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (TryMapDatabaseException(exception, out var dbProblemDetails))
        {
            _logger.LogWarning(exception, "Database exception: {Message}", exception.Message);
            httpContext.Response.StatusCode = dbProblemDetails.Status ?? StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(dbProblemDetails, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Внутренняя ошибка сервера",
            Detail = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? GetInnermostMessage(exception)
                : "Произошла непредвиденная ошибка. Попробуйте позже."
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static bool TryMapDatabaseException(Exception exception, out ProblemDetails problemDetails)
    {
        var postgresException = exception as PostgresException
            ?? (exception as DbUpdateException)?.InnerException as PostgresException;

        if (postgresException is null)
        {
            problemDetails = null!;
            return false;
        }

        problemDetails = postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Конфликт данных",
                Detail = "Запись с такими данными уже существует."
            },
            PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Некорректные данные",
                    Detail = "Переданы значения, нарушающие ограничения базы данных."
                },
            _ => null!
        };

        return problemDetails is not null;
    }

    private static string GetInnermostMessage(Exception ex)
    {
        while (ex.InnerException is not null) ex = ex.InnerException;
        return ex.Message;
    }
}

/// <summary>
/// Extension methods for mapping Result to HTTP responses.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, int successStatus = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? Results.Json(result.Value, statusCode: successStatus)
            : result.Error.ToHttpResult();

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpResult();

    private static IResult ToHttpResult(this DomainError error) =>
        error.Code switch
        {
            var c when c.StartsWith("NOT_FOUND") => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Не найдено",
                detail: error.Message),

            "AUTH_BLOCKED" => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Доступ запрещён",
                detail: error.Message),

            var c when c.StartsWith("AUTH_INVALID") || c.StartsWith("AUTH_WRONG") =>
                Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Ошибка авторизации",
                    detail: error.Message),

            "AUTH_EMAIL_TAKEN" or "PANTRY_ALREADY_EXISTS" or "FAVORITE_ALREADY_EXISTS" =>
                Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Конфликт",
                    detail: error.Message),

            _ => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ошибка",
                detail: error.Message)
        };
}

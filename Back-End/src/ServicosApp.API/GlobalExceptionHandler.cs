using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using ServicosApp.Application.Exceptions;

namespace ServicosApp.API;

public sealed class GlobalExceptionHandler : IExceptionHandler
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
        _logger.LogError(exception, "Erro não tratado. TraceId: {TraceId}", httpContext.TraceIdentifier);

        var statusCode = exception switch
        {
            AppValidationException => StatusCodes.Status400BadRequest,
            AppUnauthorizedException => StatusCodes.Status401Unauthorized,
            AppNotFoundException => StatusCodes.Status404NotFound,
            AppConflictException => StatusCodes.Status409Conflict,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                400 => "Requisição inválida",
                401 => "Não autorizado",
                404 => "Recurso não encontrado",
                409 => "Conflito",
                _ => "Erro interno no servidor"
            },
            Detail = statusCode == 500
                ? "Ocorreu um erro interno. Tente novamente mais tarde."
                : exception.Message,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}

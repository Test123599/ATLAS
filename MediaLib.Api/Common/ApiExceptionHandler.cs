using System.Net;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Common;

/// <summary>Centralized error handling: consistent JSON, no stack traces in production.</summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, message, errors) = exception switch
        {
            AppException app => (app.StatusCode, app.Code, app.Message, app.Errors),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "CONCURRENCE",
                "La ressource a été modifiée par une autre opération. Veuillez réessayer.", null),
            DbUpdateException => (StatusCodes.Status409Conflict, "BASE_DE_DONNEES",
                "L'opération n'a pas pu être enregistrée (données dupliquées ou contrainte violée).", null),
            OperationCanceledException when !httpContext.RequestAborted.IsCancellationRequested =>
                (StatusCodes.Status503ServiceUnavailable, "INDISPONIBLE", "Le service est momentanément indisponible.", null),
            _ => (StatusCodes.Status500InternalServerError, "ERREUR_INTERNE",
                "Une erreur inattendue s'est produite. Veuillez réessayer plus tard.", null),
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled API error on {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation(exception, "Business error {Code} on {Path}", code, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var traceId = httpContext.TraceIdentifier;
        if (_environment.IsProduction() && status >= 500)
        {
            traceId = traceId.Length > 8 ? traceId[..8] : traceId;
        }

        var payload = new ApiError(status, code, message, traceId, errors);
        await httpContext.Response.WriteAsJsonAsync(payload, cancellationToken);
        return true;
    }
}

/// <summary>Converts automatic ModelState validation failures to the same ApiError shape.</summary>
public static class ApiValidationResponse
{
    public static Microsoft.AspNetCore.Mvc.IActionResult Create(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var entry in context.ModelState)
        {
            var messages = entry.Value?.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).ToArray()
                           ?? [];
            if (messages.Length > 0)
            {
                errors[entry.Key] = messages;
            }
        }

        var error = new ApiError(
            StatusCodes.Status400BadRequest,
            "VALIDATION",
            "La requête contient des données invalides.",
            context.HttpContext.TraceIdentifier,
            errors);

        return new Microsoft.AspNetCore.Mvc.ObjectResult(error) { StatusCode = StatusCodes.Status400BadRequest };
    }
}

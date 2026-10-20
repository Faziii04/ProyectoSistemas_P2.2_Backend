using Practica22.Api.Common;
using Practica22.Api.Contracts;

namespace Practica22.Api.Middleware;

/// <summary>
/// Traduce las excepciones del dominio a respuestas HTTP con un cuerpo único
/// (<see cref="ApiErrorResponse"/>). Los endpoints no manejan errores.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message, ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message, null);
        }
        catch (ConflictException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "CONFLICT", ex.Message, null);
        }
        catch (ConvexApiException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message, ex.Details);
        }
        catch (BadHttpRequestException ex)
        {
            // JSON mal formado o tipo de dato incorrecto (por ejemplo price: "abc").
            await WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST_BODY",
                "El cuerpo de la petición no es válido o los tipos de datos no coinciden.",
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado en {Path}.", context.Request.Path);
            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "Ocurrió un error inesperado en el servidor.",
                null);
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string error,
        string message,
        object? details)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            Details = details,
        });
    }
}

namespace Practica22.Api.Common;

/// <summary>Error de validación de datos de entrada (HTTP 400).</summary>
public sealed class ValidationException(IReadOnlyList<ValidationError> errors)
    : Exception("Los datos enviados no son válidos.")
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors;
}

public sealed record ValidationError(string Field, string Message);

/// <summary>Recurso inexistente (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Regla de negocio incumplida, por ejemplo un SKU duplicado (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Error devuelto por la capa Convex; conserva el código y el estado HTTP.</summary>
public sealed class ConvexApiException(string code, string message, int statusCode, object? details = null)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
    public object? Details { get; } = details;
}

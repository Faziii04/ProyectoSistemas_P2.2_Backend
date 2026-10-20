using Practica22.Api.Common;

namespace Practica22.Api.Validation;

/// <summary>
/// PATRÓN CHAIN OF RESPONSIBILITY.
/// Cada eslabón valida un aspecto de la entrada y se lo pasa al siguiente.
/// A diferencia de un cortocircuito, la cadena completa se ejecuta para poder
/// devolver todos los errores de una sola vez.
/// </summary>
public abstract class ValidationHandler<T>
{
    private ValidationHandler<T>? _next;

    /// <summary>
    /// Agrega el siguiente eslabón al final de la cadena y devuelve la cabeza,
    /// para que el encadenado fluido no pierda los eslabones anteriores.
    /// </summary>
    public ValidationHandler<T> SetNext(ValidationHandler<T> next)
    {
        var tail = this;
        while (tail._next is not null)
        {
            tail = tail._next;
        }

        tail._next = next;
        return this;
    }

    public void Validate(T input, List<ValidationError> errors)
    {
        ValidateCore(input, errors);
        _next?.Validate(input, errors);
    }

    protected abstract void ValidateCore(T input, List<ValidationError> errors);
}

/// <summary>Cabeza de la cadena, con la puerta de entrada tipada.</summary>
public sealed class ValidationPipeline<T>(ValidationHandler<T> head)
{
    public IReadOnlyList<ValidationError> Validate(T input)
    {
        var errors = new List<ValidationError>();
        head.Validate(input, errors);
        return errors;
    }

    /// <summary>Lanza <see cref="ValidationException"/> (HTTP 400) si hay errores.</summary>
    public void EnsureValid(T input)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

/// <summary>Utilidades compartidas por los eslabones: validación de tipo, obligatoriedad y rango.</summary>
internal static class Rules
{
    public static void RequiredText(
        List<ValidationError> errors,
        string field,
        string? value,
        int min,
        int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new(field, "es obligatorio."));
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < min)
        {
            errors.Add(new(field, $"debe tener al menos {min} caracteres."));
        }
        else if (trimmed.Length > max)
        {
            errors.Add(new(field, $"debe tener como máximo {max} caracteres."));
        }
    }

    public static void OptionalText(
        List<ValidationError> errors,
        string field,
        string? value,
        int min,
        int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < min)
        {
            errors.Add(new(field, $"debe tener al menos {min} caracteres."));
        }
        else if (trimmed.Length > max)
        {
            errors.Add(new(field, $"debe tener como máximo {max} caracteres."));
        }
    }

    public static void RequiredNumber<T>(
        List<ValidationError> errors,
        string field,
        T? value,
        decimal min,
        decimal max)
        where T : struct, IConvertible
    {
        if (value is null)
        {
            errors.Add(new(field, "es obligatorio."));
            return;
        }

        var number = Convert.ToDecimal(value.Value);
        if (number < min || number > max)
        {
            errors.Add(new(field, $"debe estar entre {min} y {max}."));
        }
    }

    public static void OptionalNumber<T>(
        List<ValidationError> errors,
        string field,
        T? value,
        decimal min,
        decimal max)
        where T : struct, IConvertible
    {
        if (value is null)
        {
            return;
        }

        var number = Convert.ToDecimal(value.Value);
        if (number < min || number > max)
        {
            errors.Add(new(field, $"debe estar entre {min} y {max}."));
        }
    }

    public static void OneOf(
        List<ValidationError> errors,
        string field,
        string? value,
        string[] allowed,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                errors.Add(new(field, "es obligatorio."));
            }

            return;
        }

        if (!allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new(field, $"debe ser uno de: {string.Join(", ", allowed)}."));
        }
    }

    public static void Digits(
        List<ValidationError> errors,
        string field,
        string? value,
        int min,
        int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (!trimmed.All(char.IsAsciiDigit))
        {
            errors.Add(new(field, "solo puede contener números."));
        }
        else if (trimmed.Length < min || trimmed.Length > max)
        {
            errors.Add(new(field, $"debe tener entre {min} y {max} dígitos."));
        }
    }
}

using Practica22.Api.Contracts;
using Practica22.Api.Domain;

namespace Practica22.Api.Patterns;

/// <summary>
/// PATRÓN FACTORY (Simple Factory).
/// Centraliza la creación de entidades: normaliza el texto (mayúsculas en el
/// SKU, correo en minúsculas), redondea el precio y aplica valores por defecto.
/// Ningún endpoint construye entidades a mano.
/// </summary>
public static class EntityFactory
{
    public static Product CreateProduct(CreateProductRequest request) => new()
    {
        Sku = NormalizeSku(request.Sku),
        Name = request.Name!.Trim(),
        Description = request.Description?.Trim() ?? string.Empty,
        Category = request.Category!.Trim().ToLowerInvariant(),
        Price = Round2(request.Price!.Value),
        Stock = request.Stock!.Value,
        MinStock = request.MinStock!.Value,
        Active = true,
    };

    public static Customer CreateCustomer(CreateCustomerRequest request) => new()
    {
        Document = request.Document!.Trim(),
        Name = request.Name!.Trim(),
        Email = request.Email!.Trim().ToLowerInvariant(),
        Phone = request.Phone!.Trim(),
        Type = request.Type!.Trim().ToLowerInvariant(),
        Active = true,
    };

    public static string NormalizeSku(string? sku) => (sku ?? string.Empty).Trim().ToUpperInvariant();

    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

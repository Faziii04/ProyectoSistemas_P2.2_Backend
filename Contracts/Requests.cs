namespace Practica22.Api.Contracts;

/// <summary>
/// Petición para crear un producto. Todas las propiedades son nulables a
/// propósito: así la validación (tipos y rangos) la hace la cadena de
/// validadores y no el binder.
/// </summary>
public sealed record CreateProductRequest
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal? Price { get; init; }
    public int? Stock { get; init; }
    public int? MinStock { get; init; }
}

public sealed record UpdateProductRequest
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal? Price { get; init; }
    public int? Stock { get; init; }
    public int? MinStock { get; init; }
    public bool? Active { get; init; }
}

public sealed record AdjustStockRequest
{
    public int? Delta { get; init; }
}

public sealed record CreateCustomerRequest
{
    public string? Document { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Type { get; init; }
}

public sealed record UpdateCustomerRequest
{
    public string? Document { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Type { get; init; }
    public bool? Active { get; init; }
}

public sealed record SaleLineRequest
{
    public string? ProductId { get; init; }
    public int? Quantity { get; init; }
}

public sealed record CreateSaleRequest
{
    public string? CustomerId { get; init; }
    public List<SaleLineRequest>? Items { get; init; }
    public string? PaymentMethod { get; init; }
    public string? DiscountStrategy { get; init; }
    public decimal? TaxRate { get; init; }
}

/// <summary>Payload que se envía a Convex al crear una alerta de stock.</summary>
public sealed record StockAlertPayload
{
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public string Source { get; init; } = "api";
}

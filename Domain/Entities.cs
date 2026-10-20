using System.Text.Json.Serialization;

namespace Practica22.Api.Domain;

/// <summary>Claves de estrategia de descuento aceptadas por la API.</summary>
public static class DiscountStrategyKeys
{
    public const string None = "ninguno";
    public const string CustomerType = "cliente";
    public const string Volume = "volumen";
    public const string Best = "mejor";

    public static readonly string[] All = [None, CustomerType, Volume, Best];
}

public static class CustomerTypes
{
    public const string Regular = "regular";
    public const string Wholesale = "mayorista";
    public const string Vip = "vip";

    public static readonly string[] All = [Regular, Wholesale, Vip];
}

public static class PaymentMethods
{
    public const string Cash = "efectivo";
    public const string Card = "tarjeta";
    public const string Transfer = "transferencia";

    public static readonly string[] All = [Cash, Card, Transfer];
}

public static class SaleStatuses
{
    public const string Paid = "pagada";
    public const string Cancelled = "anulada";
}

/// <summary>Producto del catálogo (documento de la tabla `products` en Convex).</summary>
public sealed record Product
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public bool Active { get; init; } = true;
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; init; }

    [JsonIgnore]
    public bool LowStock => Active && Stock <= MinStock;
}

/// <summary>Cliente del sistema (documento de la tabla `customers`).</summary>
public sealed record Customer
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string Document { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Type { get; init; } = CustomerTypes.Regular;
    public bool Active { get; init; } = true;
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; init; }
}

/// <summary>Línea de una venta. Es una copia del producto al momento de la venta.</summary>
public sealed record SaleLine
{
    [JsonPropertyName("productId")]
    public string ProductId { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

/// <summary>Venta registrada (documento de la tabla `sales`).</summary>
public sealed record Sale
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string Number { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerType { get; init; } = CustomerTypes.Regular;
    public IReadOnlyList<SaleLine> Items { get; init; } = [];
    public decimal Subtotal { get; init; }
    public decimal DiscountRate { get; init; }
    public decimal Discount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public string PaymentMethod { get; init; } = PaymentMethods.Cash;
    public string Status { get; init; } = SaleStatuses.Paid;
    public long CreatedAt { get; init; }
}

/// <summary>Alerta de stock bajo persistida en Convex.</summary>
public sealed record StockAlert
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public string Source { get; init; } = "api";
    public bool Acknowledged { get; init; }
    public long CreatedAt { get; init; }
}

/// <summary>Evento que dispara el patrón Observer cuando cambia el inventario.</summary>
public sealed record StockChange(
    Product Product,
    int PreviousStock,
    int NewStock,
    string Source);

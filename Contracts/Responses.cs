using Practica22.Api.Domain;

namespace Practica22.Api.Contracts;

/// <summary>
/// Respuesta de producto. Se separa de la entidad para no exponer detalles de
/// Convex (`createdAt` en epoch) y para agregar campos derivados.
/// </summary>
public sealed record ProductResponse
{
    public string Id { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public bool Active { get; init; }
    public bool LowStock { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        Description = product.Description,
        Category = product.Category,
        Price = product.Price,
        Stock = product.Stock,
        MinStock = product.MinStock,
        Active = product.Active,
        LowStock = product.LowStock,
        CreatedAt = ToDate(product.CreatedAt),
        UpdatedAt = ToDate(product.UpdatedAt),
    };

    internal static DateTimeOffset ToDate(long epochMilliseconds) =>
        DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds);
}

public sealed record CustomerResponse
{
    public string Id { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Type { get; init; } = CustomerTypes.Regular;
    public bool Active { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id,
        Document = customer.Document,
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone,
        Type = customer.Type,
        Active = customer.Active,
        CreatedAt = ProductResponse.ToDate(customer.CreatedAt),
    };
}

public sealed record SaleLineResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed record SaleResponse
{
    public string Id { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerType { get; init; } = CustomerTypes.Regular;
    public IReadOnlyList<SaleLineResponse> Items { get; init; } = [];
    public decimal Subtotal { get; init; }
    public decimal DiscountRate { get; init; }
    public decimal Discount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public string PaymentMethod { get; init; } = PaymentMethods.Cash;
    public string Status { get; init; } = SaleStatuses.Paid;
    public DateTimeOffset CreatedAt { get; init; }

    public static SaleResponse From(Sale sale) => new()
    {
        Id = sale.Id,
        Number = sale.Number,
        CustomerId = sale.CustomerId,
        CustomerName = sale.CustomerName,
        CustomerType = sale.CustomerType,
        Items = sale.Items
            .Select(line => new SaleLineResponse
            {
                ProductId = line.ProductId,
                Sku = line.Sku,
                Name = line.Name,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                LineTotal = line.LineTotal,
            })
            .ToList(),
        Subtotal = sale.Subtotal,
        DiscountRate = sale.DiscountRate,
        Discount = sale.Discount,
        TaxRate = sale.TaxRate,
        Tax = sale.Tax,
        Total = sale.Total,
        PaymentMethod = sale.PaymentMethod,
        Status = sale.Status,
        CreatedAt = ProductResponse.ToDate(sale.CreatedAt),
    };
}

public sealed record LowStockResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public int Missing { get; init; }
}

public sealed record TopProductResponse
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed record DashboardSummaryResponse
{
    public ProductsSummary Products { get; init; } = new();
    public CustomersSummary Customers { get; init; } = new();
    public SalesSummary Sales { get; init; } = new();
    public IReadOnlyList<TopProductResponse> TopProducts { get; init; } = [];
    public IReadOnlyList<SaleResponse> RecentSales { get; init; } = [];
    public IReadOnlyList<LowStockResponse> LowStock { get; init; } = [];
}

public sealed record ProductsSummary
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int LowStock { get; init; }
    public decimal InventoryValue { get; init; }
}

public sealed record CustomersSummary
{
    public int Total { get; init; }
    public int Active { get; init; }
}

public sealed record SalesSummary
{
    public int Total { get; init; }
    public int Paid { get; init; }
    public int Cancelled { get; init; }
    public decimal Revenue { get; init; }
    public decimal TodayRevenue { get; init; }
    public int TodayCount { get; init; }
    public decimal AverageTicket { get; init; }
}

public sealed record StockAlertResponse
{
    public string Id { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public string Source { get; init; } = "api";
    public bool Acknowledged { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static StockAlertResponse From(StockAlert alert) => new()
    {
        Id = alert.Id,
        ProductId = alert.ProductId,
        Sku = alert.Sku,
        ProductName = alert.ProductName,
        Stock = alert.Stock,
        MinStock = alert.MinStock,
        Source = alert.Source,
        Acknowledged = alert.Acknowledged,
        CreatedAt = ProductResponse.ToDate(alert.CreatedAt),
    };
}

/// <summary>Respuesta estándar de error de toda la API.</summary>
public sealed record ApiErrorResponse
{
    public string Error { get; init; } = "INTERNAL_ERROR";
    public string Message { get; init; } = "Ocurrió un error inesperado.";
    public object? Details { get; init; }
}

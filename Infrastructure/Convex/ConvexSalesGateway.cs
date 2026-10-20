using Practica22.Api.Contracts;
using Practica22.Api.Domain;

namespace Practica22.Api.Infrastructure.Convex;

/// <summary>Línea que se envía al registrar una venta.</summary>
public sealed record SaleLineDraft(string ProductId, int Quantity);

/// <summary>
/// Comando de creación de venta: ya contiene el descuento resuelto por la
/// estrategia, no el nombre de la estrategia.
/// </summary>
public sealed record SaleDraft(
    string CustomerId,
    IReadOnlyList<SaleLineDraft> Items,
    string PaymentMethod,
    decimal DiscountRate,
    decimal TaxRate);

/// <summary>
/// PATRÓN ADAPTADOR (puerto del dominio).
/// Los servicios hablan sólo de productos, clientes y ventas; esta interfaz
/// oculta que del otro lado hay endpoints REST sobre Convex.
/// </summary>
public interface ISalesGateway
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? search, string? category, bool includeInactive, CancellationToken cancellationToken = default);
    Task<Product> GetProductAsync(string id, CancellationToken cancellationToken = default);
    Task<Product> CreateProductAsync(Product draft, CancellationToken cancellationToken = default);
    Task<Product> UpdateProductAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(string id, CancellationToken cancellationToken = default);
    Task<Product> AdjustStockAsync(string id, int delta, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetCustomersAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<Customer> GetCustomerAsync(string id, CancellationToken cancellationToken = default);
    Task<Customer> CreateCustomerAsync(Customer draft, CancellationToken cancellationToken = default);
    Task<Customer> UpdateCustomerAsync(string id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetSalesAsync(int limit, string? status, CancellationToken cancellationToken = default);
    Task<Sale> GetSaleAsync(string id, CancellationToken cancellationToken = default);
    Task<Sale> CreateSaleAsync(SaleDraft draft, CancellationToken cancellationToken = default);
    Task<Sale> CancelSaleAsync(string id, CancellationToken cancellationToken = default);

    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAlert>> GetAlertsAsync(int limit, CancellationToken cancellationToken = default);
    Task<StockAlert> SaveAlertAsync(StockAlertPayload payload, CancellationToken cancellationToken = default);
    Task<StockAlert> AcknowledgeAlertAsync(string id, CancellationToken cancellationToken = default);

    Task<object> HealthAsync(CancellationToken cancellationToken = default);

    Task<object> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// PATRÓN ADAPTADOR (implementación concreta).
/// Traduce las llamadas del dominio a rutas, parámetros y cuerpos de Convex,
/// y convierte la envoltura JSON en entidades de C#.
/// </summary>
public sealed class ConvexSalesGateway(ConvexApiClient client) : ISalesGateway
{
    private const string Products = "api/products";
    private const string Customers = "api/customers";
    private const string Sales = "api/sales";
    private const string Reports = "api/reports";
    private const string Health = "api/health";

    public string BaseAddress => client.BaseAddress;

    public async Task<IReadOnlyList<Product>> GetProductsAsync(
        string? search,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var list = await client.GetAsync<ConvexList<Product>>(
            Products,
            new Dictionary<string, string?>
            {
                ["q"] = search,
                ["category"] = category,
                ["includeInactive"] = includeInactive ? "true" : null,
            },
            cancellationToken);

        return list.Data;
    }

    public Task<Product> GetProductAsync(string id, CancellationToken cancellationToken = default)
        => client.GetAsync<Product>(Products + "/get", ById(id), cancellationToken);

    public Task<Product> CreateProductAsync(Product draft, CancellationToken cancellationToken = default)
        => client.SendAsync<Product>(
            HttpMethod.Post,
            Products,
            new
            {
                sku = draft.Sku,
                name = draft.Name,
                description = draft.Description,
                category = draft.Category,
                price = draft.Price,
                stock = draft.Stock,
                minStock = draft.MinStock,
            },
            cancellationToken: cancellationToken);

    public Task<Product> UpdateProductAsync(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
        => client.SendAsync<Product>(
            HttpMethod.Put,
            Products + "/update",
            new
            {
                id,
                sku = request.Sku,
                name = request.Name,
                description = request.Description,
                category = request.Category,
                price = request.Price,
                stock = request.Stock,
                minStock = request.MinStock,
                active = request.Active,
            },
            cancellationToken: cancellationToken);

    public async Task DeleteProductAsync(string id, CancellationToken cancellationToken = default)
        => await client.SendAsync<DeleteResult>(HttpMethod.Delete, Products + "/delete", ById(id), cancellationToken: cancellationToken);

    public Task<Product> AdjustStockAsync(string id, int delta, CancellationToken cancellationToken = default)
        => client.SendAsync<Product>(
            HttpMethod.Post,
            Products + "/stock",
            new { id, delta },
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var list = await client.GetAsync<ConvexList<Customer>>(
            Customers,
            new Dictionary<string, string?>
            {
                ["q"] = search,
                ["includeInactive"] = includeInactive ? "true" : null,
            },
            cancellationToken);

        return list.Data;
    }

    public Task<Customer> GetCustomerAsync(string id, CancellationToken cancellationToken = default)
        => client.GetAsync<Customer>(Customers + "/get", ById(id), cancellationToken);

    public Task<Customer> CreateCustomerAsync(Customer draft, CancellationToken cancellationToken = default)
        => client.SendAsync<Customer>(
            HttpMethod.Post,
            Customers,
            new
            {
                document = draft.Document,
                name = draft.Name,
                email = draft.Email,
                phone = draft.Phone,
                type = draft.Type,
            },
            cancellationToken: cancellationToken);

    public Task<Customer> UpdateCustomerAsync(
        string id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
        => client.SendAsync<Customer>(
            HttpMethod.Put,
            Customers + "/update",
            new
            {
                id,
                document = request.Document,
                name = request.Name,
                email = request.Email,
                phone = request.Phone,
                type = request.Type,
                active = request.Active,
            },
            cancellationToken: cancellationToken);

    public async Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default)
        => await client.SendAsync<DeleteResult>(HttpMethod.Delete, Customers + "/delete", ById(id), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Sale>> GetSalesAsync(
        int limit,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var list = await client.GetAsync<ConvexList<Sale>>(
            Sales,
            new Dictionary<string, string?>
            {
                ["limit"] = limit.ToString(),
                ["status"] = status,
            },
            cancellationToken);

        return list.Data;
    }

    public Task<Sale> GetSaleAsync(string id, CancellationToken cancellationToken = default)
        => client.GetAsync<Sale>(Sales + "/get", ById(id), cancellationToken);

    public Task<Sale> CreateSaleAsync(SaleDraft draft, CancellationToken cancellationToken = default)
        => client.SendAsync<Sale>(
            HttpMethod.Post,
            Sales,
            new
            {
                customerId = draft.CustomerId,
                items = draft.Items.Select(item => new { productId = item.ProductId, quantity = item.Quantity }),
                paymentMethod = draft.PaymentMethod,
                discountRate = draft.DiscountRate,
                taxRate = draft.TaxRate,
            },
            cancellationToken: cancellationToken);

    public Task<Sale> CancelSaleAsync(string id, CancellationToken cancellationToken = default)
        => client.SendAsync<Sale>(HttpMethod.Post, Sales + "/cancel", new { id }, cancellationToken: cancellationToken);

    public Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        => client.GetAsync<DashboardSummaryResponse>(Reports + "/summary", cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<LowStockResponse>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var list = await client.GetAsync<ConvexList<LowStockResponse>>(Reports + "/low-stock", cancellationToken: cancellationToken);
        return list.Data;
    }

    public async Task<IReadOnlyList<StockAlert>> GetAlertsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var list = await client.GetAsync<ConvexList<StockAlert>>(
            Reports + "/alerts",
            new Dictionary<string, string?> { ["limit"] = limit.ToString() },
            cancellationToken);

        return list.Data;
    }

    public Task<StockAlert> SaveAlertAsync(StockAlertPayload payload, CancellationToken cancellationToken = default)
        => client.SendAsync<StockAlert>(HttpMethod.Post, Reports + "/alerts", payload, cancellationToken: cancellationToken);

    public Task<StockAlert> AcknowledgeAlertAsync(string id, CancellationToken cancellationToken = default)
        => client.SendAsync<StockAlert>(HttpMethod.Post, Reports + "/alerts/ack", new { id }, cancellationToken: cancellationToken);

    public async Task<object> HealthAsync(CancellationToken cancellationToken = default)
        => await client.GetAsync<HealthResult>(Health, cancellationToken: cancellationToken);

    public async Task<object> SeedAsync(CancellationToken cancellationToken = default)
        => await client.SendAsync<object>(HttpMethod.Post, "api/seed", cancellationToken: cancellationToken);

    private static Dictionary<string, string?> ById(string id) => new() { ["id"] = id };

    private sealed record DeleteResult
    {
        public string Id { get; init; } = string.Empty;
        public bool Deleted { get; init; }
    }

    private sealed record HealthResult
    {
        public string Status { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;
        public long Time { get; init; }
    }
}

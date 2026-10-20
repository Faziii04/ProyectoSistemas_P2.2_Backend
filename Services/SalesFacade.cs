using Microsoft.Extensions.Options;
using Practica22.Api.Common;
using Practica22.Api.Contracts;
using Practica22.Api.Domain;
using Practica22.Api.Infrastructure.Convex;
using Practica22.Api.Patterns;
using Practica22.Api.Validation;

namespace Practica22.Api.Services;

public sealed class SalesOptions
{
    public const string SectionName = "Sales";

    /// <summary>IVA aplicado cuando la petición no envía `taxRate`.</summary>
    public decimal TaxRate { get; set; } = 0.19m;
}

public interface ISalesFacade
{
    Task<SaleResponse> CheckoutAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<DashboardSummaryResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// PATRÓN FACADE.
/// El endpoint de caja registradora sólo llama a <see cref="CheckoutAsync"/>.
/// Toda la coreografía (validación, cliente, productos, estrategia de
/// descuento, armado con el builder, persistencia y notificación a los
/// observadores) queda escondida detrás de un único punto de entrada.
/// </summary>
public sealed class SalesFacade(
    ISalesGateway gateway,
    ISaleService saleService,
    StockSubject stockSubject,
    IOptions<SalesOptions> salesOptions) : ISalesFacade
{
    private static readonly ValidationPipeline<CreateSaleRequest> SaleValidation = ValidationPipelines.CreateSale();

    public async Task<SaleResponse> CheckoutAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        SaleValidation.EnsureValid(request);

        var customer = await gateway.GetCustomerAsync(request.CustomerId!.Trim(), cancellationToken);
        if (!customer.Active)
        {
            throw new ConflictException($"El cliente {customer.Name} está inactivo.");
        }

        var strategy = DiscountStrategyFactory.Create(request.DiscountStrategy);

        var builder = new SaleBuilder()
            .ForCustomer(customer)
            .WithPaymentMethod(request.PaymentMethod!.Trim().ToLowerInvariant())
            .WithTaxRate(request.TaxRate ?? salesOptions.Value.TaxRate)
            .WithDiscount(strategy);

        var affected = new List<(Product Product, int Quantity)>();
        foreach (var line in request.Items!)
        {
            var product = await gateway.GetProductAsync(line.ProductId!.Trim(), cancellationToken);
            if (!product.Active)
            {
                throw new ConflictException($"El producto {product.Sku} está inactivo.");
            }

            builder.AddItem(product, line.Quantity!.Value);
            affected.Add((product, line.Quantity!.Value));
        }

        var sale = builder.Build();
        var created = await saleService.PersistAsync(sale, cancellationToken);

        // Observer: informar el nuevo stock de cada producto vendido.
        foreach (var (product, quantity) in affected)
        {
            await stockSubject.NotifyAsync(
                new StockChange(product, product.Stock, Math.Max(product.Stock - quantity, 0), "venta"),
                cancellationToken);
        }

        return created;
    }

    public async Task<DashboardSummaryResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var summary = await gateway.GetSummaryAsync(cancellationToken);
        var recentSales = await saleService.ListAsync(5, null, cancellationToken);
        var lowStock = await gateway.GetLowStockAsync(cancellationToken);

        return summary with
        {
            RecentSales = recentSales,
            LowStock = lowStock,
        };
    }
}

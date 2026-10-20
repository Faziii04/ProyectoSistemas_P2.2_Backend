using Practica22.Api.Common;
using Practica22.Api.Contracts;
using Practica22.Api.Domain;
using Practica22.Api.Infrastructure.Convex;

namespace Practica22.Api.Services;

public interface ISaleService
{
    Task<IReadOnlyList<SaleResponse>> ListAsync(int limit, string? status, CancellationToken cancellationToken = default);
    Task<SaleResponse> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<SaleResponse> PersistAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<SaleResponse> CancelAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>Acceso a las ventas: listar, consultar, guardar y anular.</summary>
public sealed class SaleService(ISalesGateway gateway) : ISaleService
{
    public async Task<IReadOnlyList<SaleResponse>> ListAsync(
        int limit,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var sales = await gateway.GetSalesAsync(limit <= 0 ? 50 : limit, status, cancellationToken);
        return sales.Select(SaleResponse.From).ToList();
    }

    public async Task<SaleResponse> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        return SaleResponse.From(await gateway.GetSaleAsync(id, cancellationToken));
    }

    /// <summary>Envía al gateway una venta ya construida y calculada.</summary>
    public async Task<SaleResponse> PersistAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        var draft = new SaleDraft(
            sale.CustomerId,
            sale.Items.Select(item => new SaleLineDraft(item.ProductId, item.Quantity)).ToList(),
            sale.PaymentMethod,
            sale.DiscountRate,
            sale.TaxRate);

        return SaleResponse.From(await gateway.CreateSaleAsync(draft, cancellationToken));
    }

    public async Task<SaleResponse> CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        return SaleResponse.From(await gateway.CancelSaleAsync(id, cancellationToken));
    }

    private static void EnsureId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException([new ValidationError("id", "es obligatorio.")]);
        }
    }
}

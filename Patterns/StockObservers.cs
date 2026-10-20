using Practica22.Api.Contracts;
using Practica22.Api.Domain;
using Practica22.Api.Infrastructure.Convex;

namespace Practica22.Api.Patterns;

/// <summary>
/// PATRÓN OBSERVER (sujeto observado).
/// Cualquier cambio de inventario se publica aquí y todos los observadores
/// registrados reaccionan sin que el código que vende sepa quién escucha.
/// </summary>
public interface IStockObserver
{
    Task OnStockChangedAsync(StockChange change, CancellationToken cancellationToken = default);
}

public sealed class StockSubject(IEnumerable<IStockObserver> observers, ILogger<StockSubject> logger)
{
    private readonly IReadOnlyList<IStockObserver> _observers = observers.ToList();

    /// <summary>Notifica a todos los observadores; un fallo aislado no rompe la operación de venta.</summary>
    public async Task NotifyAsync(StockChange change, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnStockChangedAsync(change, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "El observador {Observer} falló al procesar el cambio de stock de {Sku}.",
                    observer.GetType().Name,
                    change.Product.Sku);
            }
        }
    }
}

/// <summary>Observador concreto: deja traza de cada movimiento en el log.</summary>
public sealed class LoggingStockObserver(ILogger<LoggingStockObserver> logger) : IStockObserver
{
    public Task OnStockChangedAsync(StockChange change, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Stock {Sku}: {Previous} -> {Current} (mínimo {MinStock}, origen {Source})",
            change.Product.Sku,
            change.PreviousStock,
            change.NewStock,
            change.Product.MinStock,
            change.Source);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Observador concreto: cuando el stock queda en o bajo el mínimo, persiste la
/// alerta en Convex para que el frontend la muestre.
/// </summary>
public sealed class LowStockAlertObserver(ISalesGateway gateway, ILogger<LowStockAlertObserver> logger) : IStockObserver
{
    public async Task OnStockChangedAsync(StockChange change, CancellationToken cancellationToken = default)
    {
        if (!change.Product.Active || change.NewStock > change.Product.MinStock)
        {
            return;
        }

        var alert = await gateway.SaveAlertAsync(
            new StockAlertPayload
            {
                ProductId = change.Product.Id,
                Sku = change.Product.Sku,
                ProductName = change.Product.Name,
                Stock = change.NewStock,
                MinStock = change.Product.MinStock,
                Source = change.Source,
            },
            cancellationToken);

        logger.LogWarning(
            "Alerta de stock bajo para {Sku}: quedan {Stock} (mínimo {MinStock}). Alerta {AlertId}.",
            change.Product.Sku,
            change.NewStock,
            change.Product.MinStock,
            alert.Id);
    }
}

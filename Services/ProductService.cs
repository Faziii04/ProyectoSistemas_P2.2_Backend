using System.Diagnostics;
using Practica22.Api.Common;
using Practica22.Api.Contracts;
using Practica22.Api.Domain;
using Practica22.Api.Infrastructure.Convex;
using Practica22.Api.Patterns;
using Practica22.Api.Validation;

namespace Practica22.Api.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(string? search, string? category, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<ProductResponse> AdjustStockAsync(string id, AdjustStockRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Servicio de catálogo: valida, delega en el gateway y publica cambios de stock.</summary>
public sealed class ProductService(ISalesGateway gateway, StockSubject stockSubject) : IProductService
{
    private static readonly ValidationPipeline<CreateProductRequest> CreateValidation = ValidationPipelines.CreateProduct();
    private static readonly ValidationPipeline<UpdateProductRequest> UpdateValidation = ValidationPipelines.UpdateProduct();
    private static readonly ValidationPipeline<AdjustStockRequest> AdjustValidation = ValidationPipelines.AdjustStock();

    public async Task<IReadOnlyList<ProductResponse>> ListAsync(
        string? search,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var products = await gateway.GetProductsAsync(search, category, includeInactive, cancellationToken);
        return products.Select(ProductResponse.From).ToList();
    }

    public async Task<ProductResponse> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        return ProductResponse.From(await gateway.GetProductAsync(id, cancellationToken));
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        CreateValidation.EnsureValid(request);

        var draft = EntityFactory.CreateProduct(request);
        var created = await gateway.CreateProductAsync(draft, cancellationToken);

        await stockSubject.NotifyAsync(
            new StockChange(created, 0, created.Stock, "producto-creado"),
            cancellationToken);

        return ProductResponse.From(created);
    }

    public async Task<ProductResponse> UpdateAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        UpdateValidation.EnsureValid(request);

        var before = await gateway.GetProductAsync(id, cancellationToken);
        var updated = await gateway.UpdateProductAsync(id, request, cancellationToken);

        if (updated.Stock != before.Stock)
        {
            await stockSubject.NotifyAsync(
                new StockChange(updated, before.Stock, updated.Stock, "producto-editado"),
                cancellationToken);
        }

        return ProductResponse.From(updated);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        await gateway.DeleteProductAsync(id, cancellationToken);
    }

    public async Task<ProductResponse> AdjustStockAsync(string id, AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        AdjustValidation.EnsureValid(request);

        var before = await gateway.GetProductAsync(id, cancellationToken);
        var updated = await gateway.AdjustStockAsync(id, request.Delta!.Value, cancellationToken);

        await stockSubject.NotifyAsync(
            new StockChange(updated, before.Stock, updated.Stock, "ajuste-manual"),
            cancellationToken);

        return ProductResponse.From(updated);
    }

    private static void EnsureId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException([new ValidationError("id", "es obligatorio.")]);
        }
    }
}

/// <summary>
/// PATRÓN DECORATOR.
/// Envuelve al servicio real y agrega instrumentación (tiempo y errores) sin
/// modificar <see cref="ProductService"/>. Implementa la misma interfaz, así
/// que es intercambiable de forma transparente para los endpoints.
/// </summary>
public sealed class LoggingProductServiceDecorator(
    IProductService inner,
    ILogger<LoggingProductServiceDecorator> logger) : IProductService
{
    public Task<IReadOnlyList<ProductResponse>> ListAsync(string? search, string? category, bool includeInactive, CancellationToken cancellationToken = default)
        => TraceAsync("listar productos", () => inner.ListAsync(search, category, includeInactive, cancellationToken));

    public Task<ProductResponse> GetAsync(string id, CancellationToken cancellationToken = default)
        => TraceAsync($"obtener producto {id}", () => inner.GetAsync(id, cancellationToken));

    public Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        => TraceAsync($"crear producto {request.Sku}", () => inner.CreateAsync(request, cancellationToken));

    public Task<ProductResponse> UpdateAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken = default)
        => TraceAsync($"actualizar producto {id}", () => inner.UpdateAsync(id, request, cancellationToken));

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => TraceVoidAsync($"eliminar producto {id}", () => inner.DeleteAsync(id, cancellationToken));

    public Task<ProductResponse> AdjustStockAsync(string id, AdjustStockRequest request, CancellationToken cancellationToken = default)
        => TraceAsync($"ajustar stock {id}", () => inner.AdjustStockAsync(id, request, cancellationToken));

    private async Task<T> TraceAsync<T>(string operation, Func<Task<T>> action)    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            logger.LogInformation("ProductService: {Operation} completado en {Elapsed} ms.", operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning("ProductService: {Operation} falló en {Elapsed} ms ({Error}).", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private async Task TraceVoidAsync(string operation, Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action();
            logger.LogInformation("ProductService: {Operation} completado en {Elapsed} ms.", operation, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning("ProductService: {Operation} falló en {Elapsed} ms ({Error}).", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}

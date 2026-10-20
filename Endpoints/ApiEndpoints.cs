using Practica22.Api.Contracts;
using Practica22.Api.Infrastructure.Convex;
using Practica22.Api.Services;

namespace Practica22.Api.Endpoints;

/// <summary>
/// Capa de entrada HTTP. Los endpoints no validan ni calculan nada: sólo
/// delegan en los servicios y devuelven el resultado.
/// </summary>
public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        MapUtilityEndpoints(app);
        MapProductEndpoints(app);
        MapCustomerEndpoints(app);
        MapSaleEndpoints(app);
        MapReportEndpoints(app);
        return app;
    }

    private static void MapUtilityEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (ISalesGateway gateway, ConvexApiClient client, CancellationToken ct) =>
            Results.Ok(new
            {
                api = "ok",
                convex = await gateway.HealthAsync(ct),
                convexUrl = client.BaseAddress,
            }));

        app.MapPost("/api/seed", async (ISalesGateway gateway, CancellationToken ct) =>
            Results.Ok(await gateway.SeedAsync(ct)));
    }

    private static void MapProductEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products", async (
                string? q,
                string? category,
                bool? includeInactive,
                IProductService service,
                CancellationToken ct) =>
            Results.Ok(await service.ListAsync(q, category, includeInactive ?? false, ct)));

        app.MapGet("/api/products/{id}", async (string id, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, ct)));

        app.MapPost("/api/products", async (
            CreateProductRequest request,
            IProductService service,
            CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/api/products/{created.Id}", created);
        });

        app.MapPut("/api/products/{id}", async (
            string id,
            UpdateProductRequest request,
            IProductService service,
            CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(id, request, ct)));

        app.MapDelete("/api/products/{id}", async (string id, IProductService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(new { deleted = true, id });
        });

        app.MapPost("/api/products/{id}/stock", async (
            string id,
            AdjustStockRequest request,
            IProductService service,
            CancellationToken ct) =>
            Results.Ok(await service.AdjustStockAsync(id, request, ct)));
    }

    private static void MapCustomerEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers", async (
                string? q,
                bool? includeInactive,
                ICustomerService service,
                CancellationToken ct) =>
            Results.Ok(await service.ListAsync(q, includeInactive ?? false, ct)));

        app.MapGet("/api/customers/{id}", async (string id, ICustomerService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, ct)));

        app.MapPost("/api/customers", async (
            CreateCustomerRequest request,
            ICustomerService service,
            CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/api/customers/{created.Id}", created);
        });

        app.MapPut("/api/customers/{id}", async (
            string id,
            UpdateCustomerRequest request,
            ICustomerService service,
            CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(id, request, ct)));

        app.MapDelete("/api/customers/{id}", async (string id, ICustomerService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(new { deleted = true, id });
        });
    }

    private static void MapSaleEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sales", async (
                int? limit,
                string? status,
                ISaleService service,
                CancellationToken ct) =>
            Results.Ok(await service.ListAsync(limit ?? 50, status, ct)));

        app.MapGet("/api/sales/{id}", async (string id, ISaleService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, ct)));

        app.MapPost("/api/sales", async (
            CreateSaleRequest request,
            ISalesFacade facade,
            CancellationToken ct) =>
        {
            var sale = await facade.CheckoutAsync(request, ct);
            return Results.Created($"/api/sales/{sale.Id}", sale);
        });

        app.MapPost("/api/sales/{id}/cancel", async (string id, ISaleService service, CancellationToken ct) =>
            Results.Ok(await service.CancelAsync(id, ct)));
    }

    private static void MapReportEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/summary", async (ISalesFacade facade, CancellationToken ct) =>
            Results.Ok(await facade.GetDashboardAsync(ct)));

        app.MapGet("/api/reports/low-stock", async (ISalesGateway gateway, CancellationToken ct) =>
            Results.Ok(await gateway.GetLowStockAsync(ct)));

        app.MapGet("/api/reports/alerts", async (
                int? limit,
                ISalesGateway gateway,
                CancellationToken ct) =>
            Results.Ok(await gateway.GetAlertsAsync(limit ?? 50, ct)));

        app.MapPost("/api/reports/alerts/{id}/ack", async (
            string id,
            ISalesGateway gateway,
            CancellationToken ct) =>
            Results.Ok(await gateway.AcknowledgeAlertAsync(id, ct)));
    }
}

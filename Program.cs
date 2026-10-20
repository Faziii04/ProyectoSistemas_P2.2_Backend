using System.Text.Json;
using System.Text.Json.Serialization;
using Practica22.Api.Endpoints;
using Practica22.Api.Infrastructure.Convex;
using Practica22.Api.Middleware;
using Practica22.Api.Patterns;
using Practica22.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Logs sólo a consola: el proveedor de EventLog de Windows falla si el proceso
// no tiene permiso para escribir en el registro de eventos.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Documentación OpenAPI (disponible en /openapi/v1.json con el perfil Development).
builder.Services.AddOpenApi();

// Respuestas en camelCase, igual que Convex y el frontend en TypeScript.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// --- Configuración tipada -------------------------------------------------
builder.Services
    .AddOptions<ConvexOptions>()
    .Bind(builder.Configuration.GetSection(ConvexOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BaseUrl),
        "Debe configurar 'Convex:BaseUrl' (por ejemplo https://tu-deployment.convex.site).")
    .ValidateOnStart();

builder.Services
    .AddOptions<SalesOptions>()
    .Bind(builder.Configuration.GetSection(SalesOptions.SectionName));

// --- CORS: orígenes permitidos ---------------------------------------------
// Se leen de "Cors:AllowedOrigins" (appsettings o variable de entorno), porque
// en la nube el frontend NO vive en localhost. Ejemplo en Render:
//   Cors__AllowedOrigins__0=https://mi-frontend.onrender.com
// Un único valor "*" abre la API a cualquier origen (sólo para pruebas).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(child => child.Value)
    .Prepend(builder.Configuration["Cors:AllowedOrigins"])
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:3000", "http://127.0.0.1:3000"];
}

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Contains("*"))
    {
        policy.AllowAnyOrigin();
    }
    else
    {
        policy.WithOrigins(allowedOrigins);
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}));

// --- Capa de datos: Convex -------------------------------------------------
// Singleton: un único cliente HTTP (y un único HttpClient) para toda la app.
builder.Services.AddSingleton<ConvexApiClient>();
builder.Services.AddSingleton<ISalesGateway, ConvexSalesGateway>();

// --- Patrón Observer -------------------------------------------------------
builder.Services.AddSingleton<IStockObserver, LoggingStockObserver>();
builder.Services.AddSingleton<IStockObserver, LowStockAlertObserver>();
builder.Services.AddSingleton<StockSubject>();

// --- Servicios de aplicación ----------------------------------------------
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ISalesFacade, SalesFacade>();

// Patrón Decorator: IProductService se resuelve con el servicio real envuelto
// en el decorador de logging.
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(provider => new LoggingProductServiceDecorator(
    provider.GetRequiredService<ProductService>(),
    provider.GetRequiredService<ILogger<LoggingProductServiceDecorator>>()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapApiEndpoints();

app.Run();

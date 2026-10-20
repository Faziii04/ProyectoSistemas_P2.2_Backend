using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Practica22.Api.Common;

namespace Practica22.Api.Infrastructure.Convex;

public sealed class ConvexOptions
{
    public const string SectionName = "Convex";

    /// <summary>URL del sitio de Convex (HTTP Actions). Se configura en appsettings o en variables de entorno.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// PATRÓN SINGLETON + ADAPTADOR (transporte).
/// Una única instancia vive durante toda la aplicación: mantiene un solo
/// HttpClient (evita el agotamiento de sockets) y centraliza la traducción
/// entre el mundo HTTP de Convex y las excepciones del dominio.
/// </summary>
public sealed class ConvexApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    private readonly ILogger<ConvexApiClient> _logger;

    public ConvexApiClient(IOptions<ConvexOptions> options, ILogger<ConvexApiClient> logger)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            throw new InvalidOperationException("Falta configurar 'Convex:BaseUrl'.");
        }

        _logger = logger;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 30 : settings.TimeoutSeconds),
        };
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public string BaseAddress => _http.BaseAddress?.ToString() ?? string.Empty;

    public Task<T> GetAsync<T>(
        string path,
        IDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Get, path, body: null, query, cancellationToken);

    public async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        IDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, query);

        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: _json);
        }

        _logger.LogDebug("Convex {Method} {Url}", method.Method, url);

        using var response = await _http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response, raw, url);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ConvexApiException("EMPTY_RESPONSE", "Convex devolvió una respuesta vacía.", 502);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(raw, _json)
                ?? throw new ConvexApiException("NULL_RESPONSE", "Convex devolvió un cuerpo nulo.", 502);
        }
        catch (JsonException ex)
        {
            throw new ConvexApiException(
                "INVALID_RESPONSE",
                "No fue posible interpretar la respuesta de Convex.",
                502,
                new { expected = typeof(T).Name, detail = ex.Message });
        }
    }

    private static string BuildUrl(string path, IDictionary<string, string?>? query)
    {
        var relative = path.TrimStart('/');
        if (query is null || query.Count == 0)
        {
            return relative;
        }

        var pairs = query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value));
        return QueryHelpers.AddQueryString(relative, pairs);
    }

    private static ConvexApiException ToException(HttpResponseMessage response, string raw, string url)
    {
        var status = (int)response.StatusCode;
        try
        {
            var payload = JsonSerializer.Deserialize<ConvexErrorPayload>(
                raw,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (payload is not null && !string.IsNullOrWhiteSpace(payload.Error))
            {
                return new ConvexApiException(payload.Error, payload.Message ?? payload.Error, status, payload.Details);
            }
        }
        catch (JsonException)
        {
            // respuesta no JSON: se reporta el cuerpo crudo
        }

        return new ConvexApiException(
            "CONVEX_ERROR",
            $"Convex respondió {status} para '{url}'.",
            status,
            raw.Length > 300 ? raw[..300] : raw);
    }

    public void Dispose() => _http.Dispose();

    private sealed record ConvexErrorPayload
    {
        public string? Error { get; init; }
        public string? Message { get; init; }
        public JsonElement? Details { get; init; }
    }
}

/// <summary>Envoltura de los endpoints de listado de Convex: <c>{ count, data }</c>.</summary>
public sealed record ConvexList<T>
{
    public int Count { get; init; }
    public List<T> Data { get; init; } = [];
}

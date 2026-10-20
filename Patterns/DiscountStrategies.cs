using Practica22.Api.Domain;

namespace Practica22.Api.Patterns;

/// <summary>
/// PATRÓN STRATEGY.
/// Cada estrategia calcula un porcentaje de descuento distinto sobre la venta.
/// El servicio de ventas sólo conoce la interfaz, así que agregar una promo
/// nueva no obliga a tocar el código existente (principio abierto/cerrado).
/// </summary>
public interface IDiscountStrategy
{
    string Key { get; }
    string Description { get; }

    /// <summary>Porcentaje de descuento (0 a 0.5) para el cliente y el subtotal dados.</summary>
    decimal RateFor(Customer customer, decimal subtotal, int totalUnits);
}

/// <summary>Sin descuento.</summary>
public sealed class NoDiscountStrategy : IDiscountStrategy
{
    public string Key => DiscountStrategyKeys.None;
    public string Description => "Sin descuento";
    public decimal RateFor(Customer customer, decimal subtotal, int totalUnits) => 0m;
}

/// <summary>Descuento según el tipo de cliente (regular, mayorista, VIP).</summary>
public sealed class CustomerTypeDiscountStrategy : IDiscountStrategy
{
    public string Key => DiscountStrategyKeys.CustomerType;
    public string Description => "Descuento por tipo de cliente (mayorista 10 %, VIP 15 %)";

    public decimal RateFor(Customer customer, decimal subtotal, int totalUnits) => customer.Type switch
    {
        CustomerTypes.Wholesale => 0.10m,
        CustomerTypes.Vip => 0.15m,
        _ => 0m,
    };
}

/// <summary>Descuento por volumen: desde 5 unidades, 5 %.</summary>
public sealed class VolumeDiscountStrategy : IDiscountStrategy
{
    public string Key => DiscountStrategyKeys.Volume;
    public string Description => "Descuento por volumen (5 % desde 5 unidades)";

    public decimal RateFor(Customer customer, decimal subtotal, int totalUnits)
        => totalUnits >= 5 ? 0.05m : 0m;
}

/// <summary>
/// PATRÓN COMPOSITE: combina las estrategias anteriores y aplica la mejor.
/// Es, a la vez, una estrategia más, por lo que el resto del código no
/// distingue una estrategia simple de una compuesta.
/// </summary>
public sealed class BestDiscountStrategy(IEnumerable<IDiscountStrategy> strategies) : IDiscountStrategy
{
    private readonly IReadOnlyList<IDiscountStrategy> _strategies =
        strategies.Where(s => s.Key != DiscountStrategyKeys.Best).ToList();

    public string Key => DiscountStrategyKeys.Best;
    public string Description => "Aplica el mayor descuento disponible";

    public decimal RateFor(Customer customer, decimal subtotal, int totalUnits)
        => _strategies.Count == 0
            ? 0m
            : _strategies.Max(strategy => strategy.RateFor(customer, subtotal, totalUnits));
}

/// <summary>
/// PATRÓN FACTORY METHOD (junto con Strategy): resuelve qué estrategia usar a
/// partir de la clave recibida en la petición.
/// </summary>
public static class DiscountStrategyFactory
{
    public static IDiscountStrategy Create(string? key)
    {
        var normalized = string.IsNullOrWhiteSpace(key)
            ? DiscountStrategyKeys.Best
            : key.Trim().ToLowerInvariant();

        return normalized switch
        {
            DiscountStrategyKeys.None => new NoDiscountStrategy(),
            DiscountStrategyKeys.CustomerType => new CustomerTypeDiscountStrategy(),
            DiscountStrategyKeys.Volume => new VolumeDiscountStrategy(),
            DiscountStrategyKeys.Best => new BestDiscountStrategy([
                new CustomerTypeDiscountStrategy(),
                new VolumeDiscountStrategy(),
            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Estrategia de descuento desconocida."),
        };
    }
}

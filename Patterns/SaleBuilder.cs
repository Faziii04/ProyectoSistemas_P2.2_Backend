using Practica22.Api.Common;
using Practica22.Api.Domain;

namespace Practica22.Api.Patterns;

/// <summary>
/// PATRÓN BUILDER.
/// Construye la venta paso a paso: acumula líneas, aplica la estrategia de
/// descuento y calcula los totales en <see cref="Build"/>. Evita un
/// constructor de diez parámetros y garantiza que la venta quede consistente.
/// </summary>
public sealed class SaleBuilder
{
    private readonly List<SaleLine> _lines = [];
    private Customer? _customer;
    private IDiscountStrategy _discountStrategy = new NoDiscountStrategy();
    private string _paymentMethod = PaymentMethods.Cash;
    private decimal _taxRate = 0.19m;

    public SaleBuilder ForCustomer(Customer customer)
    {
        _customer = customer;
        return this;
    }

    public SaleBuilder WithPaymentMethod(string paymentMethod)
    {
        _paymentMethod = paymentMethod;
        return this;
    }

    public SaleBuilder WithTaxRate(decimal taxRate)
    {
        _taxRate = taxRate;
        return this;
    }

    public SaleBuilder WithDiscount(IDiscountStrategy strategy)
    {
        _discountStrategy = strategy;
        return this;
    }

    /// <summary>Agrega una línea calculando el total a partir del precio del producto.</summary>
    public SaleBuilder AddItem(Product product, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationException([new ValidationError("items", $"La cantidad de {product.Sku} debe ser mayor que cero.")]);
        }

        if (quantity > product.Stock)
        {
            throw new ConflictException(
                $"Stock insuficiente para {product.Sku}: disponible {product.Stock}, solicitado {quantity}.");
        }

        _lines.Add(new SaleLine
        {
            ProductId = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            UnitPrice = product.Price,
            Quantity = quantity,
            LineTotal = EntityFactory.Round2(product.Price * quantity),
        });

        return this;
    }

    public decimal Subtotal => EntityFactory.Round2(_lines.Sum(line => line.LineTotal));

    public int TotalUnits => _lines.Sum(line => line.Quantity);

    /// <summary>Porcentaje de descuento que la estrategia resuelve para esta venta.</summary>
    public decimal DiscountRate =>
        _customer is null ? 0m : _discountStrategy.RateFor(_customer, Subtotal, TotalUnits);

    public Sale Build()
    {
        if (_customer is null)
        {
            throw new ValidationException([new ValidationError("customerId", "Debe indicar el cliente de la venta.")]);
        }

        if (_lines.Count == 0)
        {
            throw new ValidationException([new ValidationError("items", "La venta debe tener al menos un producto.")]);
        }

        var subtotal = Subtotal;
        var discountRate = DiscountRate;
        var discount = EntityFactory.Round2(subtotal * discountRate);
        var taxable = EntityFactory.Round2(subtotal - discount);
        var tax = EntityFactory.Round2(taxable * _taxRate);
        var total = EntityFactory.Round2(taxable + tax);

        return new Sale
        {
            CustomerId = _customer.Id,
            CustomerName = _customer.Name,
            CustomerType = _customer.Type,
            Items = _lines.ToList(),
            Subtotal = subtotal,
            DiscountRate = discountRate,
            Discount = discount,
            TaxRate = _taxRate,
            Tax = tax,
            Total = total,
            PaymentMethod = _paymentMethod,
            Status = SaleStatuses.Paid,
        };
    }
}

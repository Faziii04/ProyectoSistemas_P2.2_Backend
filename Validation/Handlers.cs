using System.Text.RegularExpressions;
using Practica22.Api.Common;
using Practica22.Api.Contracts;
using Practica22.Api.Domain;

namespace Practica22.Api.Validation;

/* ------------------------------------------------------------------ */
/*  Eslabones de la cadena de validación (Chain of Responsibility)      */
/* ------------------------------------------------------------------ */

public sealed partial class SkuFormat
{
    [GeneratedRegex("^[A-Z0-9-]{3,30}$", RegexOptions.IgnoreCase)]
    public static partial Regex Pattern();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[A-Za-z]{2,}$")]
    public static partial Regex Email();
}

/* ----------------------------- productos ----------------------------- */

public sealed class ProductRequiredFieldsHandler : ValidationHandler<CreateProductRequest>
{
    protected override void ValidateCore(CreateProductRequest input, List<ValidationError> errors)
    {
        Rules.RequiredText(errors, "sku", input.Sku, 3, 30);
        Rules.RequiredText(errors, "name", input.Name, 3, 120);
        Rules.RequiredText(errors, "category", input.Category, 3, 40);
        Rules.OptionalText(errors, "description", input.Description, 0, 300);
    }
}

public sealed class ProductNumericRangesHandler : ValidationHandler<CreateProductRequest>
{
    protected override void ValidateCore(CreateProductRequest input, List<ValidationError> errors)
    {
        Rules.RequiredNumber(errors, "price", input.Price, 1m, 100_000_000m);
        Rules.RequiredNumber(errors, "stock", input.Stock, 0m, 100_000m);
        Rules.RequiredNumber(errors, "minStock", input.MinStock, 0m, 100_000m);
    }
}

public sealed class ProductSkuFormatHandler : ValidationHandler<CreateProductRequest>
{
    protected override void ValidateCore(CreateProductRequest input, List<ValidationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(input.Sku) && !SkuFormat.Pattern().IsMatch(input.Sku.Trim()))
        {
            errors.Add(new("sku", "solo admite letras, números y guiones (ejemplo: ELE-001)."));
        }
    }
}

public sealed class ProductUpdateHandler : ValidationHandler<UpdateProductRequest>
{
    protected override void ValidateCore(UpdateProductRequest input, List<ValidationError> errors)
    {
        if (input.Sku is null && input.Name is null && input.Description is null
            && input.Category is null && input.Price is null && input.Stock is null
            && input.MinStock is null && input.Active is null)
        {
            errors.Add(new("body", "debe enviar al menos un campo a modificar."));
            return;
        }

        if (input.Sku is not null)
        {
            if (string.IsNullOrWhiteSpace(input.Sku))
            {
                errors.Add(new("sku", "no puede estar vacío."));
            }
            else if (!SkuFormat.Pattern().IsMatch(input.Sku.Trim()))
            {
                errors.Add(new("sku", "solo admite letras, números y guiones (ejemplo: ELE-001)."));
            }
        }

        Rules.OptionalText(errors, "name", input.Name, 3, 120);
        Rules.OptionalText(errors, "category", input.Category, 3, 40);
        Rules.OptionalText(errors, "description", input.Description, 0, 300);
        Rules.OptionalNumber(errors, "price", input.Price, 1m, 100_000_000m);
        Rules.OptionalNumber(errors, "stock", input.Stock, 0m, 100_000m);
        Rules.OptionalNumber(errors, "minStock", input.MinStock, 0m, 100_000m);
    }
}

/* ----------------------------- clientes ------------------------------ */

public sealed class CustomerRequiredFieldsHandler : ValidationHandler<CreateCustomerRequest>
{
    protected override void ValidateCore(CreateCustomerRequest input, List<ValidationError> errors)
    {
        Rules.RequiredText(errors, "document", input.Document, 5, 20);
        Rules.RequiredText(errors, "name", input.Name, 3, 120);
        Rules.RequiredText(errors, "email", input.Email, 6, 120);
        Rules.RequiredText(errors, "phone", input.Phone, 7, 20);
        Rules.OneOf(errors, "type", input.Type, CustomerTypes.All, required: true);
    }
}

public sealed class CustomerFormatHandler : ValidationHandler<CreateCustomerRequest>
{
    protected override void ValidateCore(CreateCustomerRequest input, List<ValidationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(input.Email) && !SkuFormat.Email().IsMatch(input.Email.Trim()))
        {
            errors.Add(new("email", "no tiene un formato de correo válido."));
        }

        Rules.Digits(errors, "document", input.Document, 5, 20);
        Rules.Digits(errors, "phone", input.Phone, 7, 20);
    }
}

public sealed class CustomerUpdateHandler : ValidationHandler<UpdateCustomerRequest>
{
    protected override void ValidateCore(UpdateCustomerRequest input, List<ValidationError> errors)
    {
        if (input.Document is null && input.Name is null && input.Email is null
            && input.Phone is null && input.Type is null && input.Active is null)
        {
            errors.Add(new("body", "debe enviar al menos un campo a modificar."));
            return;
        }

        Rules.OptionalText(errors, "document", input.Document, 5, 20);
        Rules.OptionalText(errors, "name", input.Name, 3, 120);
        Rules.OptionalText(errors, "email", input.Email, 6, 120);
        Rules.OptionalText(errors, "phone", input.Phone, 7, 20);
        Rules.OneOf(errors, "type", input.Type, CustomerTypes.All, required: false);

        if (!string.IsNullOrWhiteSpace(input.Email) && !SkuFormat.Email().IsMatch(input.Email.Trim()))
        {
            errors.Add(new("email", "no tiene un formato de correo válido."));
        }

        Rules.Digits(errors, "document", input.Document, 5, 20);
        Rules.Digits(errors, "phone", input.Phone, 7, 20);
    }
}

/* ------------------------------ ventas ------------------------------- */

public sealed class SaleHeaderHandler : ValidationHandler<CreateSaleRequest>
{
    protected override void ValidateCore(CreateSaleRequest input, List<ValidationError> errors)
    {
        Rules.RequiredText(errors, "customerId", input.CustomerId, 1, 64);
        Rules.OneOf(errors, "paymentMethod", input.PaymentMethod, PaymentMethods.All, required: true);
        Rules.OneOf(errors, "discountStrategy", input.DiscountStrategy, DiscountStrategyKeys.All, required: false);
    }
}

public sealed class SaleLinesHandler : ValidationHandler<CreateSaleRequest>
{
    protected override void ValidateCore(CreateSaleRequest input, List<ValidationError> errors)
    {
        var items = input.Items;
        if (items is null || items.Count == 0)
        {
            errors.Add(new("items", "debe incluir al menos un producto."));
            return;
        }

        if (items.Count > 50)
        {
            errors.Add(new("items", "no puede incluir más de 50 líneas."));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < items.Count; index++)
        {
            var line = items[index];
            if (line is null)
            {
                errors.Add(new($"items[{index}]", "la línea no puede ser nula."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.ProductId))
            {
                errors.Add(new($"items[{index}].productId", "es obligatorio."));
            }
            else if (!seen.Add(line.ProductId.Trim()))
            {
                errors.Add(new($"items[{index}].productId", "el producto está repetido; sume las cantidades."));
            }

            Rules.RequiredNumber(errors, $"items[{index}].quantity", line.Quantity, 1m, 1_000m);
        }
    }
}

public sealed class SaleTotalsHandler : ValidationHandler<CreateSaleRequest>
{
    protected override void ValidateCore(CreateSaleRequest input, List<ValidationError> errors)
    {
        Rules.OptionalNumber(errors, "taxRate", input.TaxRate, 0m, 0.5m);
    }
}

public sealed class AdjustStockHandler : ValidationHandler<AdjustStockRequest>
{
    protected override void ValidateCore(AdjustStockRequest input, List<ValidationError> errors)
    {
        if (input.Delta is null)
        {
            errors.Add(new("delta", "es obligatorio."));
            return;
        }

        if (input.Delta == 0)
        {
            errors.Add(new("delta", "no puede ser cero."));
        }
        else if (Math.Abs(input.Delta.Value) > 100_000)
        {
            errors.Add(new("delta", "no puede superar 100000 unidades."));
        }
    }
}

/* ------------------------------------------------------------------ */
/*  Ensamblado de las cadenas (fábrica de pipelines)                    */
/* ------------------------------------------------------------------ */

public static class ValidationPipelines
{
    public static ValidationPipeline<CreateProductRequest> CreateProduct() => new(
        new ProductRequiredFieldsHandler()
            .SetNext(new ProductNumericRangesHandler())
            .SetNext(new ProductSkuFormatHandler()));

    public static ValidationPipeline<UpdateProductRequest> UpdateProduct() => new(
        new ProductUpdateHandler());

    public static ValidationPipeline<CreateCustomerRequest> CreateCustomer() => new(
        new CustomerRequiredFieldsHandler()
            .SetNext(new CustomerFormatHandler()));

    public static ValidationPipeline<UpdateCustomerRequest> UpdateCustomer() => new(
        new CustomerUpdateHandler());

    public static ValidationPipeline<CreateSaleRequest> CreateSale() => new(
        new SaleHeaderHandler()
            .SetNext(new SaleLinesHandler())
            .SetNext(new SaleTotalsHandler()));

    public static ValidationPipeline<AdjustStockRequest> AdjustStock() => new(
        new AdjustStockHandler());
}

using Practica22.Api.Common;
using Practica22.Api.Contracts;
using Practica22.Api.Infrastructure.Convex;
using Practica22.Api.Patterns;
using Practica22.Api.Validation;

namespace Practica22.Api.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerResponse>> ListAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<CustomerResponse> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResponse> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class CustomerService(ISalesGateway gateway) : ICustomerService
{
    private static readonly ValidationPipeline<CreateCustomerRequest> CreateValidation = ValidationPipelines.CreateCustomer();
    private static readonly ValidationPipeline<UpdateCustomerRequest> UpdateValidation = ValidationPipelines.UpdateCustomer();

    public async Task<IReadOnlyList<CustomerResponse>> ListAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var customers = await gateway.GetCustomersAsync(search, includeInactive, cancellationToken);
        return customers.Select(CustomerResponse.From).ToList();
    }

    public async Task<CustomerResponse> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        return CustomerResponse.From(await gateway.GetCustomerAsync(id, cancellationToken));
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        CreateValidation.EnsureValid(request);
        return CustomerResponse.From(await gateway.CreateCustomerAsync(EntityFactory.CreateCustomer(request), cancellationToken));
    }

    public async Task<CustomerResponse> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        UpdateValidation.EnsureValid(request);
        return CustomerResponse.From(await gateway.UpdateCustomerAsync(id, request, cancellationToken));
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureId(id);
        await gateway.DeleteCustomerAsync(id, cancellationToken);
    }

    private static void EnsureId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException([new ValidationError("id", "es obligatorio.")]);
        }
    }
}

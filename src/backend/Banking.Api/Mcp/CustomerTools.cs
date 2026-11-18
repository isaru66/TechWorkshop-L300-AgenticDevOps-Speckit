using System.ComponentModel;
using Banking.Api.Contracts;
using Banking.Api.Controllers;
using Banking.Api.Services;
using ModelContextProtocol.Server;

namespace Banking.Api.Mcp;

[McpServerToolType]
public sealed class CustomerTools(BankingService service)
{
    [McpServerTool(Name = "list_customers"), Description("List customers, paged (pageSize 1-100).")]
    public Task<PageResult<CustomerDto>> ListCustomers(
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Items per page, 1-100.")] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.CustomersAsync(page, pageSize, cancellationToken));

    [McpServerTool(Name = "get_customer"), Description("Get one customer by ID.")]
    public Task<CustomerDto> GetCustomer(
        [Description("Customer UUID.")] string customerId, CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.CustomerAsync(RouteId.Parse(customerId), cancellationToken));

    [McpServerTool(Name = "create_customer"), Description("Create a customer. Name is required.")]
    public Task<CustomerDto> CreateCustomer(
        [Description("Customer name (required, not blank).")] string name,
        [Description("English name.")] string? englishName = null,
        [Description("Email address.")] string? email = null,
        [Description("Phone number.")] string? phone = null,
        [Description("Address line.")] string? addressLine = null,
        [Description("Subdistrict.")] string? subdistrict = null,
        [Description("District.")] string? district = null,
        [Description("Province.")] string? province = null,
        [Description("Postcode.")] string? postcode = null,
        [Description("Date of birth, yyyy-MM-dd.")] DateOnly? dateOfBirth = null,
        [Description("Occupation.")] string? occupation = null,
        [Description("National ID.")] string? nationalId = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.CreateCustomerAsync(new CustomerWriteRequest(name, englishName, email, phone,
            addressLine, subdistrict, district, province, postcode, dateOfBirth, occupation, nationalId), cancellationToken));

    [McpServerTool(Name = "update_customer"), Description("Update a customer. Only supplied fields change.")]
    public Task<CustomerDto> UpdateCustomer(
        [Description("Customer UUID.")] string customerId,
        [Description("New name (cannot be blank).")] string? name = null,
        [Description("English name.")] string? englishName = null,
        [Description("Email address.")] string? email = null,
        [Description("Phone number.")] string? phone = null,
        [Description("Address line.")] string? addressLine = null,
        [Description("Subdistrict.")] string? subdistrict = null,
        [Description("District.")] string? district = null,
        [Description("Province.")] string? province = null,
        [Description("Postcode.")] string? postcode = null,
        [Description("Date of birth, yyyy-MM-dd.")] DateOnly? dateOfBirth = null,
        [Description("Occupation.")] string? occupation = null,
        [Description("National ID.")] string? nationalId = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Run(() => service.UpdateCustomerAsync(RouteId.Parse(customerId), new CustomerWriteRequest(name, englishName,
            email, phone, addressLine, subdistrict, district, province, postcode, dateOfBirth, occupation, nationalId), cancellationToken));

    [McpServerTool(Name = "delete_customer"), Description("Delete a customer. Fails with has_dependents if the customer owns accounts.")]
    public Task<object> DeleteCustomer(
        [Description("Customer UUID.")] string customerId, CancellationToken cancellationToken = default) =>
        McpErrors.RunDelete(() => service.DeleteCustomerAsync(RouteId.Parse(customerId), cancellationToken));
}

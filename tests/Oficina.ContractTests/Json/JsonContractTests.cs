using System.Text.Json;
using MediatR;
using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Users.Dtos;
using Oficina.ContractTests.Fixtures;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Identity.Enums;

namespace Oficina.ContractTests.Json;

[Collection(nameof(ContractCollection))]
public sealed class JsonContractTests(ContractApiFactory factory)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 30, 12, 30, 15, TimeSpan.Zero);

    [Fact]
    public async Task CustomerPage_UsesCamelCaseStringEnumsIsoDatesAndThePaginationShape()
    {
        var customer = new CustomerDto(Guid.CreateVersion7(), "Maria", "52998224725", DocumentType.Cpf, null, "11987654321", new AddressDto("01310100", "Av", "1", null, "Centro", "SP", "SP"), new MessagingConsentDto(true, false, CreatedAt), CreatedAt, null);
        factory.Sender.Send(Arg.Any<IRequest<Result<PagedResult<CustomerDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<CustomerDto>(new[] { customer }, 1, 20, 1)));
        using var client = factory.CreateClient(Role.Admin);

        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/admin/customers?page=1&pageSize=20", TestContext.Current.CancellationToken));

        var root = json.RootElement;
        Assert.Equal(new[] { "items", "page", "pageSize", "totalItems" }, root.EnumerateObject().Select(property => property.Name));
        var item = root.GetProperty("items")[0];
        Assert.Equal("Cpf", item.GetProperty("documentType").GetString());
        Assert.Equal("2026-09-30T12:30:15+00:00", item.GetProperty("createdAt").GetString());
        Assert.Equal("01310100", item.GetProperty("address").GetProperty("zipCode").GetString());
        Assert.True(item.GetProperty("messagingConsent").GetProperty("whatsApp").GetBoolean());
        Assert.Equal("2026-09-30T12:30:15+00:00", item.GetProperty("messagingConsent").GetProperty("updatedAt").GetString());
        Assert.All(item.EnumerateObject(), property => Assert.True(char.IsLower(property.Name[0]), property.Name));
    }

    [Fact]
    public async Task User_SerializesTheRoleAsStringAndNeverThePasswordHash()
    {
        var user = new UserDto(Guid.CreateVersion7(), "Ana", "ana@oficina.com", Role.Mechanic, true, UserStatus.InvitationPending, CreatedAt, CreatedAt, CreatedAt);
        factory.Sender.Send(Arg.Any<IRequest<Result<UserDto>>>(), Arg.Any<CancellationToken>()).Returns(Result.Success(user));
        using var client = factory.CreateClient(Role.Admin);

        var body = await client.GetStringAsync($"/api/admin/users/{user.Id}", TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(body);
        Assert.Equal("Mechanic", json.RootElement.GetProperty("role").GetString());
        Assert.Equal("InvitationPending", json.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }
}

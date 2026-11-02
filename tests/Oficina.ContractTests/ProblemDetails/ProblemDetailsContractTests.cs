using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Users.Dtos;
using Oficina.ContractTests.Fixtures;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.ContractTests.ProblemDetails;

[Collection(nameof(ContractCollection))]
public sealed class ProblemDetailsContractTests(ContractApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ValidationFailure_Returns400WithFieldErrors()
    {
        var fields = new[] { new FieldError("document", CustomerErrors.InvalidDocument.Code, CustomerErrors.InvalidDocument.Message) };
        ReturnCustomerError(Error.Validation(fields));
        using var client = factory.CreateClient(Role.Attendant);

        using var response = await client.PostAsJsonAsync("/api/admin/customers/search", new { document = "123" }, CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code);
        var error = Assert.Single(problem.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Equal("document", error.GetProperty("field").GetString());
        Assert.Equal(CustomerErrors.InvalidDocument.Code, error.GetProperty("code").GetString());
        Assert.Equal(CustomerErrors.InvalidDocument.Message, error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task MalformedJson_Returns400WithInvalidFormatFields()
    {
        using var client = factory.CreateClient(Role.Admin);
        using var content = new StringContent("{\"name\":\"Ana\",\"role\":\"Boss\"}", Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/api/admin/users/{Guid.CreateVersion7()}", content, CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code);
        Assert.Contains(problem.RootElement.GetProperty("errors").EnumerateArray(), error =>
            error.GetProperty("code").GetString() == CommonErrors.InvalidFormat.Code);
    }

    [Fact]
    public async Task MissingToken_Returns401()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/customers", CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, CommonErrors.Unauthorized.Code);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-token");

        using var response = await client.GetAsync("/api/auth/me", CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, CommonErrors.Unauthorized.Code);
    }

    [Fact]
    public async Task WrongRole_Returns403()
    {
        using var client = factory.CreateClient(Role.Mechanic);

        using var response = await client.GetAsync("/api/admin/customers", CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, CommonErrors.Forbidden.Code);
    }

    [Fact]
    public async Task NotFound_Returns404()
    {
        ReturnCustomerError(CustomerErrors.NotFound);
        using var client = factory.CreateClient(Role.Admin);

        using var response = await client.GetAsync($"/api/admin/customers/{Guid.CreateVersion7()}", CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, CustomerErrors.NotFound.Code);
    }

    [Fact]
    public async Task Conflict_Returns409()
    {
        ReturnCustomerError(CustomerErrors.DuplicateDocument);
        using var client = factory.CreateClient(Role.Admin);

        using var response = await client.PostAsJsonAsync("/api/admin/customers", new { name = "Maria", document = "52998224725", phone = "11987654321" }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, CustomerErrors.DuplicateDocument.Code);
    }

    [Fact]
    public async Task BusinessRule_Returns422()
    {
        factory.Sender.Send(Arg.Any<IRequest<Result<UserDto>>>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserDto>.Failure(IdentityErrors.CannotManageOwnAccount));
        using var client = factory.CreateClient(Role.Admin);

        using var response = await client.PutAsJsonAsync($"/api/admin/users/{Guid.CreateVersion7()}", new { name = "Ana", role = "Mechanic" }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, IdentityErrors.CannotManageOwnAccount.Code);
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutInternalDetails()
    {
        factory.Sender.Send(Arg.Any<IRequest<Result<CustomerDto>>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SELECT * FROM customers WHERE document = '52998224725'"));
        using var client = factory.CreateClient(Role.Admin);

        using var response = await client.GetAsync($"/api/admin/customers/{Guid.CreateVersion7()}", CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError, CommonErrors.Unexpected.Code);
        var body = problem.RootElement.GetRawText();
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", body, StringComparison.Ordinal);
        Assert.DoesNotContain("52998224725", body, StringComparison.Ordinal);
        Assert.Equal(CommonErrors.Unexpected.Message, problem.RootElement.GetProperty("detail").GetString());
    }

    private void ReturnCustomerError(Error error) =>
        factory.Sender.Send(Arg.Any<IRequest<Result<CustomerDto>>>(), Arg.Any<CancellationToken>()).Returns(Result<CustomerDto>.Failure(error));

    private static async Task<JsonDocument> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("detail").GetString()));
        return problem;
    }
}

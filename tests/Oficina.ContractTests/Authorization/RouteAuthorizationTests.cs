using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oficina.Api.Authorization;
using Oficina.ContractTests.Fixtures;

namespace Oficina.ContractTests.Authorization;

[Collection(nameof(ContractCollection))]
public sealed class RouteAuthorizationTests(ContractApiFactory factory)
{
    private static readonly string[] AnonymousAllowlist = { "api/auth/login", "api/auth/invitations/inspect", "api/auth/invitations/accept", "health" };

    [Fact]
    public void FallbackPolicy_RequiresAnAuthenticatedUser()
    {
        var options = factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(options.FallbackPolicy.Requirements, requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void OnlyTheAllowlist_AcceptsAnonymousRequests()
    {
        foreach (var endpoint in Endpoints())
        {
            var route = Route(endpoint);
            var isAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            Assert.True(isAnonymous == AnonymousAllowlist.Contains(route), $"{route} anonymous={isAnonymous}");
        }
    }

    [Fact]
    public void EveryProtectedRoute_DeclaresAnExplicitPolicy()
    {
        var protectedEndpoints = Endpoints().Where(endpoint => !AnonymousAllowlist.Contains(Route(endpoint))).ToArray();

        Assert.NotEmpty(protectedEndpoints);
        foreach (var endpoint in protectedEndpoints)
        {
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy).ToArray();
            Assert.True(policies.Length > 0 && policies.All(policy => policy is not null), Route(endpoint));
            if (Route(endpoint).StartsWith("api/admin/", StringComparison.Ordinal))
            {
                Assert.All(policies, policy => Assert.Contains(policy, new[] { Policies.Authenticated, Policies.AdminOnly, Policies.AdminOrManager, Policies.Staff }));
            }
        }
    }

    [Fact]
    public void UserAdministration_ChangesAreAdminOnlyAndReadsAllowTheManager()
    {
        var userEndpoints = Endpoints().Where(endpoint => Route(endpoint).StartsWith("api/admin/users", StringComparison.Ordinal)).ToArray();

        Assert.Equal(10, userEndpoints.Length);
        foreach (var endpoint in userEndpoints)
        {
            var isRead = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.SequenceEqual(new[] { "GET" }) == true;
            Assert.Equal(isRead ? Policies.AdminOrManager : Policies.AdminOnly, endpoint.Metadata.GetMetadata<IAuthorizeData>()?.Policy);
        }
    }

    private static string Route(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText!.Trim('/');

    private RouteEndpoint[] Endpoints() =>
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
}

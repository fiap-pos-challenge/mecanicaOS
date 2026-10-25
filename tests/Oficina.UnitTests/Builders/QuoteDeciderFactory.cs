using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Settings;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.Domain.Settings.Repositories;
using Oficina.Domain.Team.Repositories;

namespace Oficina.UnitTests.Builders;

public static class QuoteDeciderFactory
{
    public static QuoteDecider Create(IServiceOrderRepository serviceOrders, IPartRepository parts, ICurrentUser currentUser, IEstimateRepository? estimates = null, IQuoteQueries? queries = null)
    {
        var pricing = new QuotePricing(new SettingsStore(Substitute.For<ISettingsRepository>(), currentUser, TimeProvider.System), Substitute.For<IUserRepository>(), Substitute.For<ITeamProfileRepository>(), currentUser);
        var scopes = new EstimateScopeResolver(Substitute.For<IServiceRepository>(), parts, Substitute.For<IPackageRepository>());
        return new QuoteDecider(estimates ?? Substitute.For<IEstimateRepository>(), serviceOrders, queries ?? Substitute.For<IQuoteQueries>(), pricing, scopes, new QuoteApprovalPolicy(parts), currentUser, TimeProvider.System);
    }
}

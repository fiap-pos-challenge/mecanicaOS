using Oficina.Application.Abstractions.Security;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Quotes.Pricing;
using Oficina.Domain.Team.Repositories;

namespace Oficina.Application.Quotes.Pricing;

public sealed class QuotePricing(SettingsStore settings, IUserRepository users, ITeamProfileRepository profiles, ICurrentUser currentUser)
{
    public Task<DiscountSettings> DiscountsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);

    public Task<PaymentSettings> PaymentsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);

    public async Task<PricingTerms> TermsAsync(CustomerFacts? facts, CancellationToken cancellationToken) =>
        Terms(await DiscountsAsync(cancellationToken), facts, PricingTerms.NoLimit);

    public async Task<PricingTerms> TermsWithUserLimitAsync(CustomerFacts? facts, CancellationToken cancellationToken)
    {
        var discounts = await DiscountsAsync(cancellationToken);
        return Terms(discounts, facts, await LimitAsync(discounts, cancellationToken));
    }

    public static PricingTerms Terms(DiscountSettings discounts, CustomerFacts? facts, decimal limitPercent) =>
        new(discounts.Rules.Select(rule => new DiscountRule(rule.Type, rule.Value, rule.Condition, rule.IsActive)).ToList(), facts ?? CustomerFacts.None, limitPercent);

    public async Task<decimal> LimitAsync(DiscountSettings discounts, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return 0;
        }

        if (await profiles.GetAsync(userId, cancellationToken) is { UpdatedAt: not null } profile)
        {
            return profile.DiscountLimitPercent;
        }

        return discounts.Limits.FirstOrDefault(limit => limit.Role == user.Role.ToString())?.Percent ?? 0;
    }

    public async Task<string> CurrentNameAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId && await users.GetByIdAsync(userId, cancellationToken) is { } user ? user.Name : QuoteTexts.System;
}

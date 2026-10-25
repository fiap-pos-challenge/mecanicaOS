using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Pricing;

namespace Oficina.UnitTests.Domain.Quotes;

public sealed class EstimateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly PricingTerms NoRules = new([], CustomerFacts.None, PricingTerms.NoLimit);

    private static EstimateLine Line(string name = "Alinhamento", decimal price = 100m, string kind = EstimateLineKind.Service) =>
        new(Guid.CreateVersion7(), kind, Guid.CreateVersion7(), name, price, 1m, "Manual", false);

    private static Estimate Draft()
    {
        var estimate = Estimate.Open(1, Guid.CreateVersion7(), ["Pix"], 3);
        Assert.True(estimate.SaveDraft([Line(), Line("Balanceamento")], DiscountMode.Automatic, null, ["Pix", "Credit"], 6, Now).IsSuccess);
        return estimate;
    }

    [Fact]
    public void Draft_IsEditableUntilDelivered_AndReopensWhenTheLinkExpiresOrFails()
    {
        var estimate = Draft();
        Assert.Equal(2, estimate.Lines.Count);
        Assert.Equal(6, estimate.MaxInstallments);
        Assert.True(estimate.IsEditable(Now));
        Assert.True(estimate.EnsureEditable(Now).IsSuccess);

        estimate.Deliver("Link", "http://localhost/o/x", new string('a', 64), Now.AddDays(7));
        Assert.False(estimate.IsEditable(Now));
        Assert.Equal(QuoteErrors.Locked, estimate.EnsureEditable(Now).Error);
        Assert.Equal(QuoteErrors.Locked, estimate.SaveDraft([Line()], DiscountMode.Automatic, null, ["Pix"], 1, Now).Error);
        Assert.Equal(QuoteErrors.Locked, estimate.RequestApproval(new ManualDiscount("Percent", 10, "Cliente fiel"), "Ana", NoRules, Now).Error);
        Assert.Equal(QuoteErrors.Locked, estimate.EnsureReadyToSend(NoRules, Now).Error);
        Assert.Equal(QuoteErrors.RetryNotAllowed, estimate.EnsureRetry(Now).Error);

        estimate.MarkDelivery(false, "Sem e-mail", Now);
        Assert.Equal(DeliveryStatus.Failed, estimate.Delivery!.Status);
        Assert.True(estimate.IsEditable(Now));
        Assert.True(estimate.EnsureRetry(Now).IsSuccess);
        Assert.True(estimate.SaveDraft([Line()], DiscountMode.Manual, new ManualDiscount("Percent", 5, "Fiel"), ["Pix"], 1, Now).IsSuccess);
        Assert.Null(estimate.Delivery);
        Assert.Equal("Fiel", estimate.ManualDiscount!.Reason);
    }

    [Fact]
    public void SaveDraft_RejectsInvalidQuantitiesAndNonPositiveManualDiscount_AndNormalizesTheMode()
    {
        var estimate = Draft();
        var fractional = Line() with { Quantity = 1.5m };

        Assert.Equal(QuoteErrors.InvalidQuantity, estimate.SaveDraft([fractional], DiscountMode.Automatic, null, ["Pix"], 1, Now).Error);
        Assert.Equal(QuoteErrors.InvalidDiscount, estimate.SaveDraft([Line()], DiscountMode.Manual, new ManualDiscount("Percent", 0, "Fiel"), ["Pix"], 1, Now).Error);
        Assert.True(estimate.SaveDraft([Line()], "Unknown", new ManualDiscount("Percent", 5, "Fiel"), ["Pix"], 1, Now).IsSuccess);
        Assert.Equal(DiscountMode.Automatic, estimate.DiscountMode);
        Assert.Null(estimate.ManualDiscount);
    }

    [Fact]
    public void Delivery_IsMarkedSentThenOpened()
    {
        var estimate = Draft();
        estimate.Deliver("Email", "http://localhost/o/x", new string('b', 64), Now.AddDays(3));
        estimate.MarkDelivery(true, null, Now);
        Assert.Equal(DeliveryStatus.Sent, estimate.Delivery!.Status);

        estimate.MarkOpened(Now.AddHours(1));
        Assert.Equal(DeliveryStatus.Opened, estimate.Delivery.Status);
        Assert.Equal(Now.AddHours(1), estimate.Delivery.OpenedAt);
        estimate.Deliver("Link", "http://localhost/o/y", new string('c', 64), Now.AddDays(3));
        Assert.Equal(2, estimate.Delivery.Attempts);
    }

    [Fact]
    public void PortalAttempts_CountBeforeTheCheck_LockAfterThree_AndResetOnConfirmation()
    {
        var estimate = Draft();
        Assert.Equal(PortalErrors.LinkUnavailable, estimate.RegisterPortalAttempt(Now).Error);
        estimate.Deliver("Link", "http://localhost/o/x", new string('a', 64), Now.AddDays(3));

        Assert.True(estimate.RegisterPortalAttempt(Now).IsSuccess);
        Assert.Equal(2, estimate.PortalAttemptsLeft);
        estimate.ConfirmPortalIdentity(Now);
        Assert.Equal(0, estimate.PortalFailures);
        Assert.Equal(DeliveryStatus.Opened, estimate.Delivery!.Status);

        for (var attempt = 0; attempt < Estimate.MaxPortalAttempts; attempt++)
        {
            Assert.True(estimate.RegisterPortalAttempt(Now).IsSuccess);
        }

        Assert.True(estimate.IsPortalLocked);
        Assert.Equal(0, estimate.PortalAttemptsLeft);
        Assert.Equal(PortalErrors.Locked, estimate.RegisterPortalAttempt(Now).Error);
        Assert.Equal(PortalErrors.Locked, estimate.EnsurePortalLinkOpen(Now).Error);
        Assert.Equal(PortalErrors.LinkUnavailable, estimate.EnsurePortalLinkOpen(Now.AddDays(4)).Error);
    }

    [Fact]
    public void DiscountApproval_IsOnlyForDiscountsAboveTheLimit_AndIsDecidedOnce()
    {
        var estimate = Draft();
        var limited = new PricingTerms([], CustomerFacts.None, 10);

        Assert.Equal(QuoteErrors.InvalidDiscount, estimate.RequestApproval(new ManualDiscount("Percent", 120, "Frota grande"), "Ana", limited, Now).Error);
        Assert.Equal(QuoteErrors.DiscountReason, estimate.RequestApproval(new ManualDiscount("Percent", 25, "ab"), "Ana", limited, Now).Error);
        Assert.Equal(QuoteErrors.ApprovalNotNeeded, estimate.RequestApproval(new ManualDiscount("Percent", 5, "Cliente fiel"), "Ana", limited, Now).Error);
        Assert.True(estimate.RequestApproval(new ManualDiscount("Percent", 25, "  Frota grande  "), "Ana", limited, Now).IsSuccess);
        var approval = estimate.Approval!;
        Assert.Equal("Frota grande", approval.Reason);

        Assert.Equal(QuoteErrors.NotFound, estimate.DecideApproval(Guid.CreateVersion7(), true, "Gabi").Error);
        Assert.True(estimate.DecideApproval(approval.Id, true, "Gabi").IsSuccess);
        Assert.Equal(DiscountApproval.Approved, estimate.Approval!.Status);
        Assert.Equal(QuoteErrors.ApprovalDecided, estimate.DecideApproval(approval.Id, false, "Gabi").Error);
    }

    [Fact]
    public void EnsureReadyToSend_BlocksEmptyZeroAndUnapprovedDiscounts()
    {
        var empty = Estimate.Open(2, Guid.CreateVersion7(), ["Pix"], 1);
        Assert.Equal(QuoteErrors.Empty, empty.EnsureReadyToSend(NoRules, Now).Error);

        var free = Estimate.Open(3, Guid.CreateVersion7(), ["Pix"], 1);
        free.SaveDraft([Line(price: 0)], DiscountMode.None, null, ["Pix"], 1, Now);
        Assert.Equal(QuoteErrors.ZeroTotal, free.EnsureReadyToSend(NoRules, Now).Error);

        var manual = Draft();
        manual.SaveDraft(manual.Lines, DiscountMode.Manual, new ManualDiscount("Percent", 30, "ab"), ["Pix"], 1, Now);
        Assert.Equal(QuoteErrors.DiscountReason, manual.EnsureReadyToSend(NoRules, Now).Error);

        var limited = new PricingTerms([], CustomerFacts.None, 10);
        manual.SaveDraft(manual.Lines, DiscountMode.Manual, new ManualDiscount("Percent", 30, "Frota grande"), ["Pix"], 1, Now);
        Assert.Equal(QuoteErrors.ApprovalRequired, manual.EnsureReadyToSend(limited, Now).Error);
        manual.RequestApproval(new ManualDiscount("Percent", 30, "Frota grande"), "Ana", limited, Now);
        manual.DecideApproval(manual.Approval!.Id, true, "Gabi");
        Assert.True(manual.EnsureReadyToSend(limited, Now).IsSuccess);
        Assert.True(Draft().EnsureReadyToSend(NoRules, Now).IsSuccess);
    }

    [Fact]
    public void Approve_Partially_KeepsOnlyTheAcceptedLines_WithoutRecordingARejection()
    {
        var estimate = Draft();
        var first = estimate.Lines.First();

        var accepted = estimate.Approve([first.Id], NoRules);

        Assert.True(accepted.IsSuccess);
        Assert.Equal(new[] { first }, accepted.Value);
        Assert.Equal(new[] { first.Id }, estimate.AcceptedLineIds);
        Assert.Equal(EstimateStatus.Approved, estimate.Status);
        Assert.Null(estimate.RejectionReason);
        Assert.True(estimate.IsPartiallyApproved);
        Assert.Equal(new[] { first }, estimate.EffectiveLines);
        Assert.Equal(100m, estimate.Price(NoRules).Total);
        Assert.Equal(QuoteErrors.NotPending, estimate.Approve(null, NoRules).Error);
        Assert.Equal(QuoteErrors.NotPending, estimate.Reject("Muito caro").Error);
        Assert.Equal(QuoteErrors.NotPending, estimate.EnsureCanBeAnswered().Error);
        Assert.Equal(QuoteErrors.VersionNotAllowed, estimate.NextVersion().Error);
    }

    [Fact]
    public void Approve_WithoutChoice_AcceptsEveryLine()
    {
        var estimate = Draft();
        Assert.True(estimate.EnsureCanBeAnswered().IsSuccess);

        Assert.True(estimate.Approve(null, NoRules).IsSuccess);
        Assert.Equal(estimate.Lines.Select(line => line.Id), estimate.AcceptedLineIds);
        Assert.False(estimate.IsPartiallyApproved);
    }

    [Fact]
    public void Approve_FailsWithoutItemsOrWithZeroTotal()
    {
        var estimate = Draft();
        Assert.Equal(QuoteErrors.NoItemsAccepted, estimate.Approve([Guid.CreateVersion7()], NoRules).Error);

        var free = Estimate.Open(4, Guid.CreateVersion7(), ["Pix"], 1);
        free.SaveDraft([Line(price: 0)], DiscountMode.None, null, ["Pix"], 1, Now);
        Assert.Equal(QuoteErrors.ZeroTotal, free.Approve(null, NoRules).Error);
        Assert.Equal(EstimateStatus.Pending, free.Status);
    }

    [Fact]
    public void AmountToCharge_IsTheApprovedTotalUntilPaid()
    {
        var estimate = Draft();
        Assert.Equal(PortalErrors.NothingToPay, estimate.AmountToCharge(false, NoRules).Error);

        estimate.Approve(null, NoRules);
        Assert.Equal(200m, estimate.AmountToCharge(false, NoRules).Value);
        Assert.Equal(PortalErrors.NothingToPay, estimate.AmountToCharge(true, NoRules).Error);
    }

    [Fact]
    public void Reject_NeedsAReason_AndAllowsANewVersionWithNewLineIds()
    {
        var estimate = Draft();

        Assert.Equal(QuoteErrors.RejectReason, estimate.Reject("ab").Error);
        Assert.True(estimate.Reject("  Vou pensar  ").IsSuccess);
        Assert.Equal("Vou pensar", estimate.RejectionReason);

        var next = estimate.NextVersion();
        Assert.True(next.IsSuccess);
        Assert.Equal(2, next.Value.Version);
        Assert.Equal(estimate.Number, next.Value.Number);
        Assert.Equal(EstimateStatus.Pending, next.Value.Status);
        Assert.DoesNotContain(next.Value.Lines, line => estimate.Lines.Any(old => old.Id == line.Id));
        Assert.Equal(estimate.PaymentMethods, next.Value.PaymentMethods);
    }

    [Fact]
    public void Lines_CannotBeChangedOutsideTheAggregate()
    {
        var estimate = Draft();

        Assert.IsNotType<List<EstimateLine>>(estimate.Lines);
        Assert.IsNotType<List<string>>(estimate.PaymentMethods);
        Assert.IsNotType<List<Guid>>(estimate.AcceptedLineIds);
    }
}

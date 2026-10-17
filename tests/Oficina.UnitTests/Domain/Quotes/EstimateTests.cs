using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;

namespace Oficina.UnitTests.Domain.Quotes;

public sealed class EstimateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private static EstimateLine Line(string name = "Alinhamento") => new(Guid.CreateVersion7(), "Service", Guid.CreateVersion7(), name, 100m, 1m, "Mechanic", false);

    private static Estimate Draft()
    {
        var estimate = Estimate.Open(1, Guid.CreateVersion7(), ["Pix"], 3);
        Assert.True(estimate.SaveDraft([Line(), Line("Balanceamento")], "Automatic", null, ["Pix", "Credit"], 6, Now).IsSuccess);
        return estimate;
    }

    [Fact]
    public void Draft_IsEditableUntilDelivered_AndReopensWhenTheLinkExpiresOrFails()
    {
        var estimate = Draft();
        Assert.Equal(2, estimate.Lines.Count);
        Assert.Equal(6, estimate.MaxInstallments);
        Assert.True(estimate.IsEditable(Now));

        estimate.Deliver("Link", "http://localhost/o/x", new string('a', 64), Now.AddDays(7));
        Assert.False(estimate.IsEditable(Now));
        Assert.Equal(QuoteErrors.Locked, estimate.SaveDraft([Line()], "Automatic", null, ["Pix"], 1, Now).Error);
        Assert.Equal(QuoteErrors.Locked, estimate.RequestApproval("Percent", 10, "Cliente fiel", "Ana", Now).Error);
        Assert.Equal(QuoteErrors.RetryNotAllowed, estimate.EnsureRetry(Now).Error);

        estimate.MarkDelivery(false, "Sem e-mail", Now);
        Assert.Equal(DeliveryStatus.Failed, estimate.Delivery!.Status);
        Assert.True(estimate.IsEditable(Now));
        Assert.True(estimate.EnsureRetry(Now).IsSuccess);
        Assert.True(estimate.SaveDraft([Line()], "Manual", new ManualDiscount("Percent", 5, "Fiel"), ["Pix"], 1, Now).IsSuccess);
        Assert.Null(estimate.Delivery);
        Assert.Equal("Fiel", estimate.ManualDiscount!.Reason);
    }

    [Fact]
    public void Delivery_IsMarkedSentThenOpened_AndPortalFailuresReset()
    {
        var estimate = Draft();
        estimate.Deliver("Email", "http://localhost/o/x", new string('b', 64), Now.AddDays(3));
        estimate.MarkDelivery(true, null, Now);
        Assert.Equal(DeliveryStatus.Sent, estimate.Delivery!.Status);

        estimate.RegisterPortalFailure();
        estimate.RegisterPortalFailure();
        Assert.Equal(2, estimate.PortalFailures);
        estimate.ResetPortalFailures();
        Assert.Equal(0, estimate.PortalFailures);

        estimate.MarkOpened(Now.AddHours(1));
        Assert.Equal(DeliveryStatus.Opened, estimate.Delivery.Status);
        Assert.Equal(Now.AddHours(1), estimate.Delivery.OpenedAt);
        estimate.Deliver("Link", "http://localhost/o/y", new string('c', 64), Now.AddDays(3));
        Assert.Equal(2, estimate.Delivery.Attempts);
    }

    [Fact]
    public void DiscountApproval_IsDecidedOnce()
    {
        var estimate = Draft();
        Assert.True(estimate.RequestApproval("Percent", 25, "  Frota grande  ", "Ana", Now).IsSuccess);
        var approval = estimate.Approval!;
        Assert.Equal("Frota grande", approval.Reason);

        Assert.Equal(QuoteErrors.NotFound, estimate.DecideApproval(Guid.CreateVersion7(), true, "Gabi").Error);
        Assert.True(estimate.DecideApproval(approval.Id, true, "Gabi").IsSuccess);
        Assert.Equal("Approved", estimate.Approval!.Status);
        Assert.Equal(QuoteErrors.ApprovalDecided, estimate.DecideApproval(approval.Id, false, "Gabi").Error);
    }

    [Fact]
    public void Approve_KeepsTheAcceptedLines_AndOnlyOnce()
    {
        var estimate = Draft();
        var first = estimate.Lines[0].Id;

        Assert.True(estimate.Approve([first]).IsSuccess);
        Assert.Equal([first], estimate.AcceptedLineIds);
        Assert.Equal(EstimateStatus.Approved, estimate.Status);
        Assert.Equal(QuoteErrors.NotPending, estimate.Approve().Error);
        Assert.Equal(QuoteErrors.NotPending, estimate.Reject("Muito caro").Error);
        Assert.Equal(QuoteErrors.VersionNotAllowed, estimate.NextVersion().Error);
    }

    [Fact]
    public void Approve_WithoutChoice_AcceptsEveryLine()
    {
        var estimate = Draft();

        Assert.True(estimate.Approve().IsSuccess);
        Assert.Equal(estimate.Lines.Select(line => line.Id), estimate.AcceptedLineIds);
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
}

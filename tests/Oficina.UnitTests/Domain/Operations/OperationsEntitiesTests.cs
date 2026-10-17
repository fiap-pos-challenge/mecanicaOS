using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Automations;
using Oficina.Domain.Dashboard;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Jobs.Entities;
using Oficina.Domain.Notifications;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Reports;

namespace Oficina.UnitTests.Domain.Operations;

public sealed class OperationsEntitiesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PortalSession_LastsOneHour()
    {
        var session = PortalSession.Start(new string('a', 64), Guid.CreateVersion7(), Now);

        Assert.Equal(Now.AddHours(1), session.ExpiresAt);
    }

    [Fact]
    public void PortalCharge_ExpiresWhilePending_AndIsPaidOnlyOnce()
    {
        var charge = PortalCharge.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Pix", 150m, "TX1", Now.AddMinutes(30));

        Assert.Equal("Pending", charge.Refresh(Now));
        Assert.True(charge.MarkPaid(Now));
        Assert.Equal(Now, charge.PaidAt);
        Assert.False(charge.MarkPaid(Now.AddMinutes(1)));
        Assert.Equal("Paid", charge.Refresh(Now.AddHours(2)));

        var late = PortalCharge.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), "Pix", 10m, "TX2", Now);
        Assert.Equal("Expired", late.Refresh(Now));
        Assert.False(late.MarkPaid(Now));
    }

    [Fact]
    public void IssuedDocument_KeepsTheSnapshot()
    {
        var snapshot = new DocumentSnapshot(7, 2, "Oficina", "Maria", "BRA2E19", [new DocumentLine("Óleo", 4, 58, 232)], 0, 232, 0, 232, null, ["Pix"]);
        var document = IssuedDocument.Issue("ORC-7-ABC", "Quote", Guid.CreateVersion7(), Now, snapshot);

        Assert.Equal("ORC-7-ABC", document.Code);
        Assert.Equal(232m, document.Snapshot.Total);
        Assert.Equal(Now, document.IssuedAt);
    }

    [Fact]
    public void AuditEntry_TrimsAndCutsLongTexts()
    {
        var entry = AuditEntry.Record(Now, null, "  Ana  ", "security", new string('x', 300), "alvo", " detalhe ");

        Assert.Equal("Ana", entry.ActorName);
        Assert.Equal(AuditEntry.TextMaxLength, entry.Action.Length);
        Assert.Equal("detalhe", entry.Detail);
    }

    [Fact]
    public void BackgroundJob_CompletesOrFails()
    {
        var job = BackgroundJob.Queue(BackgroundJob.CustomerImport, Guid.CreateVersion7(), null, Now);
        Assert.Equal("Queued", job.Status);
        job.Complete("arquivo.csv", "{}", Now.AddSeconds(5));
        Assert.Equal("Completed", job.Status);
        Assert.Equal("arquivo.csv", job.FileName);

        var failed = BackgroundJob.Queue(BackgroundJob.CustomerDataExport, Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        failed.Fail(Now);
        Assert.Equal("Failed", failed.Status);
        Assert.Equal(Now, failed.CompletedAt);
    }

    [Fact]
    public void AutomationRule_UpdatesAndTogglesItself()
    {
        var rule = AutomationRule.Create("QuoteSent", "{}", Now);
        Assert.True(rule.Enabled);

        rule.Update("PaymentReceived", "{\"name\":\"x\"}");
        rule.SetEnabled(false);
        Assert.Equal("PaymentReceived", rule.Trigger);
        Assert.False(rule.Enabled);
    }

    [Fact]
    public void AutomationExecution_FinishesFailsAndRetries()
    {
        var execution = AutomationExecution.Schedule(Guid.CreateVersion7(), "QuoteSent", Guid.CreateVersion7(), "quote:1", Now.AddDays(2), Now);
        Assert.Equal(ExecutionStatus.Pending, execution.Status);

        execution.Finish(ExecutionStatus.Failed, new string('s', 400), "Sem fornecedor", Now);
        Assert.Equal(1, execution.Attempts);
        Assert.Equal(AutomationExecution.TextMaxLength, execution.Summary.Length);
        Assert.Equal("Sem fornecedor", execution.Error);

        execution.Retry(Now.AddMinutes(5));
        Assert.Equal(ExecutionStatus.Pending, execution.Status);
        Assert.Null(execution.Error);
        Assert.Null(execution.FinishedAt);
        Assert.Equal(Now.AddMinutes(5), execution.DueAt);
    }

    [Fact]
    public void Notification_CutsTitleAndBody()
    {
        var notification = Notification.Create("StockLow", new string('t', 200), new string('b', 600), "/inventory", true, "stock:1", Now);

        Assert.Equal(Notification.TitleMaxLength, notification.Title.Length);
        Assert.Equal(Notification.BodyMaxLength, notification.Body.Length);
        Assert.True(notification.AiSuggested);
        var read = NotificationRead.Mark(notification.Id, Guid.CreateVersion7(), Now);
        Assert.Equal(notification.Id, read.NotificationId);
    }

    [Fact]
    public void SavedReport_ScheduleResetsTheLastSending()
    {
        var report = SavedReport.Create("Faturamento", "{}", Now);
        report.MarkSent(Now);
        report.Update("Faturamento mensal", "{\"a\":1}", Now.AddDays(1));
        report.Schedule("{\"weekday\":\"Monday\"}", Now.AddDays(1));

        Assert.Equal("Faturamento mensal", report.Name);
        Assert.Null(report.LastSentAt);
        Assert.NotNull(report.ScheduleJson);
    }

    [Fact]
    public void DashboardAction_RecordsWhoActed()
    {
        var userId = Guid.CreateVersion7();
        var action = DashboardAction.Record("stock-1", "CreatePurchaseOrder", userId, Now);

        Assert.Equal("stock-1", action.ItemId);
        Assert.Equal(userId, action.UserId);
    }
}

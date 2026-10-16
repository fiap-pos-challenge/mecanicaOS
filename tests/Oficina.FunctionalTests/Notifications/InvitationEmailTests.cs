using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Infrastructure.Notifications;

namespace Oficina.FunctionalTests.Notifications;

public sealed class InvitationEmailTests
{
    private static readonly InvitationMessage Message = new("Ana <Lima>", "ana@oficina.com", "a+b/c", new DateTimeOffset(2026, 10, 8, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Link_PutsTheTokenInTheFragment() =>
        Assert.Equal("http://localhost:4200/invite#token=a%2Bb%2Fc", InvitationEmail.Link("http://localhost:4200/invite", Message.Token));

    [Fact]
    public void Body_EncodesTheNameAndShowsTheExpiryInBrasiliaTime()
    {
        var body = InvitationEmail.Body(Message, "http://localhost:4200/invite#token=x");

        Assert.Contains("Ana &lt;Lima&gt;", body, StringComparison.Ordinal);
        Assert.Contains("08/10/2026 às 12:00", body, StringComparison.Ordinal);
        Assert.Contains("href=\"http://localhost:4200/invite#token=x\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WithoutSmtpHost_SkipsTheDelivery()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions()), NullLogger<SmtpEmailSender>.Instance);

        await sender.SendInvitationAsync(Message, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Send_UnreachableServer_DoesNotThrow()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions { SmtpHost = "127.0.0.1", SmtpPort = 1 }), NullLogger<SmtpEmailSender>.Instance);

        await sender.SendInvitationAsync(Message, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Send_WithPickupDirectory_WritesOneEmlFileWithTheInvitation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"oficina-mail-{Guid.NewGuid():N}");
        try
        {
            var sender = new SmtpEmailSender(Options.Create(new EmailOptions { PickupDirectory = directory }), NullLogger<SmtpEmailSender>.Instance);

            await sender.SendInvitationAsync(Message, TestContext.Current.CancellationToken);

            var file = Assert.Single(Directory.GetFiles(directory, "*.eml"));
            var content = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            Assert.Contains("To: ana@oficina.com", content, StringComparison.Ordinal);
            Assert.Contains("Subject:", content, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Send_WithPickupDirectoryAndSmtpHost_PrefersThePickupDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"oficina-mail-{Guid.NewGuid():N}");
        try
        {
            var settings = new EmailOptions { PickupDirectory = directory, SmtpHost = "127.0.0.1", SmtpPort = 1 };
            var sender = new SmtpEmailSender(Options.Create(settings), NullLogger<SmtpEmailSender>.Instance);

            await sender.SendInvitationAsync(Message, TestContext.Current.CancellationToken);

            Assert.Single(Directory.GetFiles(directory, "*.eml"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Send_PickupDirectoryUnavailable_DoesNotThrow()
    {
        var blocker = Path.GetTempFileName();
        try
        {
            var sender = new SmtpEmailSender(Options.Create(new EmailOptions { PickupDirectory = blocker }), NullLogger<SmtpEmailSender>.Instance);

            await sender.SendInvitationAsync(Message, TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(blocker);
        }
    }
}

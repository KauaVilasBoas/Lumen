using AegisIdentity.Domain.Notifications;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AegisIdentity.UnitTests.Infrastructure.Notifications;

public sealed class MailKitEmailServiceTests
{
    private static readonly EmailMessage SampleMessage = new(
        To: "user@example.com",
        Subject: "Test",
        HtmlBody: "<p>hi</p>",
        TextBody: "hi");

    [Fact]
    public async Task SendAsync_OnFirstAttemptSuccess_CallsTransportOnce()
    {
        var transport = FakeSmtpTransport.AlwaysSucceeds();
        var service = CreateService(transport);

        await service.SendAsync(SampleMessage);

        transport.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WhenFirstAttemptFails_RetriesAndSucceeds()
    {
        var transport = new FakeSmtpTransport()
            .ThenThrow(new InvalidOperationException("transient SMTP glitch"))
            .ThenSucceed();
        var service = CreateService(transport);

        await service.SendAsync(SampleMessage);

        transport.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenAllAttemptsFail_SwallowsExceptionAndDoesNotThrow()
    {
        var transport = new FakeSmtpTransport()
            .ThenThrow(new InvalidOperationException("attempt 1"))
            .ThenThrow(new InvalidOperationException("attempt 2"));
        var service = CreateService(transport);

        var act = async () => await service.SendAsync(SampleMessage);

        await act.Should().NotThrowAsync();
        transport.CallCount.Should().Be(MailKitEmailService.MaxAttempts);
    }

    [Fact]
    public async Task SendAsync_OnTimeoutException_StillRetriesAndFailsOpen()
    {
        var transport = new FakeSmtpTransport()
            .ThenThrow(new TimeoutException("SMTP connect timed out"))
            .ThenThrow(new TimeoutException("SMTP connect timed out"));
        var service = CreateService(transport);

        var act = async () => await service.SendAsync(SampleMessage);

        await act.Should().NotThrowAsync();
        transport.CallCount.Should().Be(MailKitEmailService.MaxAttempts);
    }

    [Fact]
    public async Task SendAsync_WhenCancellationRequested_PropagatesOperationCanceledException()
    {
        // The fail-open contract must not swallow caller-driven cancellation —
        // otherwise pipelines that race the operation against a deadline silently win.
        var transport = new FakeSmtpTransport().ThenThrow(new OperationCanceledException());
        var service = CreateService(transport);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await service.SendAsync(SampleMessage, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_BuildsMultipartMessageWithHtmlAndTextBodies()
    {
        var transport = FakeSmtpTransport.AlwaysSucceeds();
        var service = CreateService(transport);

        await service.SendAsync(SampleMessage);

        var sent = transport.Sent.Should().ContainSingle().Subject;
        sent.From.Mailboxes.Single().Address.Should().Be("no-reply@aegisidentity.local");
        sent.To.Mailboxes.Single().Address.Should().Be("user@example.com");
        sent.Subject.Should().Be("Test");
        sent.HtmlBody.Should().Contain("<p>hi</p>");
        sent.TextBody!.Trim().Should().Be("hi");
    }

    [Fact]
    public async Task SendAsync_WithOnlyTextBody_SendsTextPart()
    {
        var transport = FakeSmtpTransport.AlwaysSucceeds();
        var service = CreateService(transport);

        await service.SendAsync(new EmailMessage(
            To: "user@example.com",
            Subject: "Text only",
            HtmlBody: string.Empty,
            TextBody: "plain"));

        var sent = transport.Sent.Single();
        sent.HtmlBody.Should().BeNull();
        sent.TextBody!.Trim().Should().Be("plain");
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_Throws()
    {
        var service = CreateService(FakeSmtpTransport.AlwaysSucceeds());

        var act = async () => await service.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static MailKitEmailService CreateService(ISmtpTransport transport)
    {
        var options = Options.Create(new SmtpOptions
        {
            Host = "localhost",
            Port = 1025,
            User = string.Empty,
            Pass = string.Empty,
            From = "no-reply@aegisidentity.local",
            UseStartTls = false,
        });

        return new MailKitEmailService(
            transport,
            options,
            NullLogger<MailKitEmailService>.Instance);
    }
}

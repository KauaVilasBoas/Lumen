using AegisIdentity.Infrastructure.Notifications;
using MimeKit;

namespace AegisIdentity.UnitTests.Infrastructure.Notifications;

internal sealed class FakeSmtpTransport : ISmtpTransport
{
    private readonly Queue<Func<Task>> _behaviours = new();

    public int CallCount { get; private set; }
    public List<MimeMessage> Sent { get; } = new();

    public static FakeSmtpTransport AlwaysSucceeds() => new();

    public FakeSmtpTransport ThenSucceed()
    {
        _behaviours.Enqueue(() => Task.CompletedTask);
        return this;
    }

    public FakeSmtpTransport ThenThrow(Exception exception)
    {
        _behaviours.Enqueue(() => throw exception);
        return this;
    }

    public Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        CallCount++;
        Sent.Add(message);

        if (_behaviours.Count == 0)
            return Task.CompletedTask;

        return _behaviours.Dequeue().Invoke();
    }
}

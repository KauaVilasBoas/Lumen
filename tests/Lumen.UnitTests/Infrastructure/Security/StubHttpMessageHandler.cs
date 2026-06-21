using System.Net;

namespace AegisIdentity.UnitTests.Infrastructure.Security;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public int CallCount { get; private set; }

    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public static StubHttpMessageHandler RespondingWith(HttpStatusCode status, string body)
        => new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body),
        }));

    public static StubHttpMessageHandler Throwing(Exception exception)
        => new((_, _) => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        Requests.Add(request);
        return _handler(request, cancellationToken);
    }
}

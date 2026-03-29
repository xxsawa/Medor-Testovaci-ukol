namespace Medor.Api.Tests.TestDoubles;

/// <summary>Routes all HTTP calls through a delegate (no real network).</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) => _send = send;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_send(request));
}

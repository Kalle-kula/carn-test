using System.Net;

namespace KycAggregation.Api.Tests;

/// <summary>Routes requests to a per-path responder so tests can shape upstream behavior.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(responder(request));

    public static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    public static HttpResponseMessage NotFound()
        => new(HttpStatusCode.NotFound);
}

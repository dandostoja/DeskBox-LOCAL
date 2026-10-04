using System.Net;

namespace DeskBox.Services;

/// <summary>
/// Hard network boundary for the LOCAL build. Runtime-owned HTTP clients use
/// this transport, which never opens a socket and always returns a local 503.
/// Tests may still inject their own HttpClient explicitly.
/// </summary>
internal static class OfflineNetwork
{
    internal const string ReasonPhrase = "DeskBox LOCAL offline mode";

    internal static HttpClient CreateHttpClient(
        TimeSpan? timeout = null,
        HttpMessageHandler? injectedHandler = null)
    {
        // Production callers pass no handler and are hard-blocked offline.
        // The optional handler is an internal test seam used by the existing
        // transport unit tests; it is never supplied by normal application code.
        var client = injectedHandler is null
            ? new HttpClient(new OfflineHttpMessageHandler(), disposeHandler: true)
            : new HttpClient(injectedHandler, disposeHandler: true);
        if (timeout is { } value)
        {
            client.Timeout = value;
        }

        return client;
    }

    private sealed class OfflineHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                ReasonPhrase = ReasonPhrase,
                Content = new StringContent("Network access is disabled in DeskBox LOCAL.")
            });
        }
    }
}

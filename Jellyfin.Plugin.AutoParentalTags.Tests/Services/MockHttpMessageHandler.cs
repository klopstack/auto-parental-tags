using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Services;

/// <summary>
/// Configurable <see cref="HttpMessageHandler"/> used to unit test HTTP
/// clients without performing real network requests. Each queued response is
/// returned once, in order, and every request that is sent is recorded.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responderQueue = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Queues a response produced by the supplied responder function.
    /// </summary>
    /// <param name="responder">Function that maps the request to a response.</param>
    /// <returns>This handler, for chaining.</returns>
    public MockHttpMessageHandler RespondWith(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responderQueue.Enqueue(responder);
        return this;
    }

    /// <summary>
    /// Queues a simple JSON response with the supplied status code and body.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="json">The JSON body to return.</param>
    /// <returns>This handler, for chaining.</returns>
    public MockHttpMessageHandler RespondWithJson(HttpStatusCode statusCode, string json)
    {
        _responderQueue.Enqueue(
            _ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        return this;
    }

    /// <summary>
    /// The number of requests that have been handled so far.
    /// </summary>
    public int RequestCount => _requests.Count;

    /// <summary>
    /// The requests that have been handled so far, in order.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_responderQueue.Count == 0)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("No mock response configured")
            };
        }

        var responder = _responderQueue.Dequeue();
        var response = responder(request);

        if (response.RequestMessage is null)
        {
            response.RequestMessage = request;
        }

        return await Task.FromResult(response).ConfigureAwait(false);
    }
}

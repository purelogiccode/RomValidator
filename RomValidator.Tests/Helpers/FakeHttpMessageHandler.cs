using System.Net;

namespace RomValidator.Tests.Helpers;

/// <summary>
/// A configurable <see cref="HttpMessageHandler"/> used to simulate HTTP responses
/// in unit tests without performing real network calls.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    /// <summary>Initializes a handler that always returns the specified response.</summary>
    /// <param name="responseFactory">Factory that creates the response for each request.</param>
    public FakeHttpMessageHandler(Func<HttpResponseMessage> responseFactory)
        : this((_, _) => Task.FromResult(responseFactory()))
    {
    }

    /// <summary>Initializes a handler using a request-aware callback.</summary>
    /// <param name="handler">Callback invoked for each request.</param>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    /// <summary>Gets the number of requests received by this handler.</summary>
    public int RequestCount { get; private set; }

    /// <summary>Gets the requests received by this handler.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        Requests.Add(request);
        return _handler(request, cancellationToken);
    }

    /// <summary>Creates a JSON response with the specified status code.</summary>
    /// <param name="json">The JSON body.</param>
    /// <param name="statusCode">The HTTP status code (defaults to 200 OK).</param>
    /// <returns>The configured HTTP response.</returns>
    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

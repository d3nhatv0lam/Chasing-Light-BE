using System.Net;
using System.Text;

namespace ChasingLight.Api.Tests.Helpers;

/// <summary>
/// Giả lập HttpMessageHandler để kiểm thử các service gọi HttpClient mà không phụ thuộc mạng.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public List<HttpRequestMessage> Requests { get; } = [];

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public static MockHttpMessageHandler FromResponses(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        return new MockHttpMessageHandler((_, _) =>
        {
            var next = queue.Count > 0
                ? queue.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return Task.FromResult(next);
        });
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        Requests.Add(request);

        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return await _handler(request, cancellationToken);
    }
}

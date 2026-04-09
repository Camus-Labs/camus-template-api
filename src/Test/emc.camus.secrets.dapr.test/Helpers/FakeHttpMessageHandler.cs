using System.Net;

namespace emc.camus.secrets.dapr.test.Helpers;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode? _statusCode;
    private readonly string? _content;
    private readonly Exception? _exception;
    private readonly Dictionary<string, (HttpStatusCode statusCode, string content)>? _responses;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public FakeHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    public FakeHttpMessageHandler(Dictionary<string, (HttpStatusCode statusCode, string content)> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        if (_responses != null)
        {
            var secretName = request.RequestUri!.Segments.Last();
            if (_responses.TryGetValue(secretName, out var response))
            {
                var matchedMessage = new HttpResponseMessage(response.statusCode);
                matchedMessage.Content = new StringContent(response.content);
                return Task.FromResult(matchedMessage);
            }

            var notFoundMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            notFoundMessage.Content = new StringContent("");
            return Task.FromResult(notFoundMessage);
        }

        var resultMessage = new HttpResponseMessage(_statusCode!.Value);
        resultMessage.Content = new StringContent(_content!);
        return Task.FromResult(resultMessage);
    }
}

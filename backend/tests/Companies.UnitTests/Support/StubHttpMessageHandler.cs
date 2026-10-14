using System.Net;
using System.Text;

namespace Companies.UnitTests.Support;

/// <summary>An <see cref="HttpMessageHandler"/> that answers from a delegate and records the requests it saw.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this((request, _) => Task.FromResult(respond(request)))
    {
    }

    public List<Uri> RequestedUris { get; } = [];

    public static HttpResponseMessage Html(string html, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(html, Encoding.UTF8, "text/html"),
    };

    public static HttpResponseMessage Redirect(string location, HttpStatusCode status = HttpStatusCode.Found)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri!);
        return respond(request, cancellationToken);
    }
}

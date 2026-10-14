using System.Net;
using System.Text;
using Companies.Domain.Companies;
using Companies.Infrastructure.WebContent;
using Companies.UnitTests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Companies.UnitTests.Infrastructure;

[TestFixture]
public sealed class HttpWebsiteContentFetcherTests
{
    private static readonly WebsiteUrl Website = WebsiteUrl.Create("https://abc.xyz").Value;

    private FakeLogger<HttpWebsiteContentFetcher> _logger = null!;

    [SetUp]
    public void SetUp() => _logger = new FakeLogger<HttpWebsiteContentFetcher>();

    [Test]
    public async Task Fetch_HtmlPage_ReturnsParsedContent()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html("<title>Alphabet</title>"));

        var content = await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None);

        content.ShouldNotBeNull().Title.ShouldBe("Alphabet");
        handler.RequestedUris.ShouldBe([new Uri("https://abc.xyz")]);
    }

    [Test]
    public async Task Fetch_FollowsRelativeAndAbsoluteRedirects()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.AbsoluteUri switch
        {
            "https://abc.xyz/" => StubHttpMessageHandler.Redirect("/home", HttpStatusCode.MovedPermanently),
            "https://abc.xyz/home" => StubHttpMessageHandler.Redirect("https://www.abc.xyz/"),
            _ => StubHttpMessageHandler.Html("<title>Alphabet</title>"),
        });

        var content = await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None);

        content.ShouldNotBeNull().Title.ShouldBe("Alphabet");
        handler.RequestedUris.Select(u => u.AbsoluteUri).ShouldBe(
            ["https://abc.xyz/", "https://abc.xyz/home", "https://www.abc.xyz/"]);
    }

    [Test]
    public async Task Fetch_TooManyRedirects_ReturnsNull()
    {
        var hop = 0;
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Redirect($"https://abc.xyz/{++hop}"));

        var content = await CreateFetcher(handler, maxRedirects: 2).FetchAsync(Website, CancellationToken.None);

        content.ShouldBeNull();
        handler.RequestedUris.Count.ShouldBe(3);
        _logger.Collector.LatestRecord.Level.ShouldBe(LogLevel.Warning);
    }

    [TestCase("http://localhost/admin")]
    [TestCase("http://169.254.169.254/latest/meta-data")]
    [TestCase("file:///etc/passwd")]
    [TestCase("ftp://abc.xyz/")]
    public async Task Fetch_RedirectToDisallowedTarget_ReturnsNullWithoutFollowing(string location)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Redirect(location));

        var content = await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None);

        content.ShouldBeNull();
        handler.RequestedUris.Count.ShouldBe(1);
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task Fetch_ErrorStatus_ReturnsNull(HttpStatusCode status)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html("<title>Error</title>", status));

        (await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None)).ShouldBeNull();
    }

    [Test]
    public async Task Fetch_NonHtmlContent_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"title\":\"Alphabet\"}", Encoding.UTF8, "application/json"),
        });

        (await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None)).ShouldBeNull();
    }

    [Test]
    public async Task Fetch_LargeBody_ReadsOnlyUpToLimit()
    {
        var html = "<title>Alphabet</title>" + new string('x', 10_000) + "<h1>Beyond the limit</h1>";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html(html));

        var content = await CreateFetcher(handler, maxResponseBytes: 1024).FetchAsync(Website, CancellationToken.None);

        content.ShouldNotBeNull().ShouldSatisfyAllConditions(
            c => c.Title.ShouldBe("Alphabet"),
            c => c.Heading.ShouldBeNull());
    }

    [Test]
    public async Task Fetch_NetworkFailure_ReturnsNullAndLogsWarning()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var content = await CreateFetcher(handler).FetchAsync(Website, CancellationToken.None);

        content.ShouldBeNull();
        _logger.Collector.LatestRecord.ShouldSatisfyAllConditions(
            r => r.Level.ShouldBe(LogLevel.Warning),
            r => r.Message.ShouldContain("abc.xyz"));
    }

    [Test]
    public async Task Fetch_SlowSite_TimesOutAndReturnsNull()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return StubHttpMessageHandler.Html("<title>Too late</title>");
        });

        var content = await CreateFetcher(handler, timeout: TimeSpan.FromMilliseconds(100)).FetchAsync(Website, CancellationToken.None);

        content.ShouldBeNull();
    }

    [Test]
    public async Task Fetch_CallerCancels_PropagatesCancellation()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return StubHttpMessageHandler.Html("<title>Never</title>");
        });
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Should.ThrowAsync<OperationCanceledException>(() => CreateFetcher(handler).FetchAsync(Website, caller.Token));
    }

    private HttpWebsiteContentFetcher CreateFetcher(
        HttpMessageHandler handler, int maxRedirects = 3, int maxResponseBytes = 256 * 1024, TimeSpan? timeout = null)
    {
        var options = new WebsiteFetcherOptions
        {
            MaxRedirects = maxRedirects,
            MaxResponseBytes = maxResponseBytes,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
        };

        return new HttpWebsiteContentFetcher(new HttpClient(handler), Options.Create(options), _logger);
    }
}

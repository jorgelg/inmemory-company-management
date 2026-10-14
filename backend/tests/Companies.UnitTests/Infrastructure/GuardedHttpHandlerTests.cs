using System.Net;
using Companies.Infrastructure.WebContent;

namespace Companies.UnitTests.Infrastructure;

[TestFixture]
public sealed class GuardedHttpHandlerTests
{
    [TestCase("http://localhost:9/")]
    [TestCase("http://127.0.0.1:9/")]
    [TestCase("http://[::1]:9/")]
    public async Task Send_ToLoopback_IsBlockedBeforeConnecting(string url)
    {
        using var client = new HttpClient(GuardedHttpHandler.Create(new WebsiteFetcherOptions()));

        var exception = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(new Uri(url)));

        exception.ToString().ShouldContain("non-public address");
    }

    [Test]
    public void Create_DisablesAutomaticRedirectsAndProxy()
    {
        using var handler = GuardedHttpHandler.Create(new WebsiteFetcherOptions());

        handler.ShouldSatisfyAllConditions(
            h => h.AllowAutoRedirect.ShouldBeFalse(),
            h => h.UseProxy.ShouldBeFalse(),
            h => h.UseCookies.ShouldBeFalse(),
            h => h.AutomaticDecompression.ShouldBe(DecompressionMethods.All),
            h => h.ConnectCallback.ShouldNotBeNull());
    }
}

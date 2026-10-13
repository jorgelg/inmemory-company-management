using Companies.Domain.Companies;
using Companies.UnitTests.Support;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class WebsiteUrlTests
{
    [TestCase("https://www.example.com", "https://www.example.com")]
    [TestCase("https://www.example.com/", "https://www.example.com")]
    [TestCase("HTTPS://WWW.Example.com", "https://www.example.com")]
    [TestCase("http://example.com/about?x=1", "http://example.com/about?x=1")]
    [TestCase("https://example.com:8443/", "https://example.com:8443")]
    [TestCase("  https://example.com  ", "https://example.com")]
    [TestCase("https://example.com/#section", "https://example.com")]
    public void Create_WithValidUrl_Normalizes(string input, string expected)
    {
        WebsiteUrl.Create(input).ShouldBeSuccess().Value.ShouldBe(expected);
    }

    [Test]
    public void Create_ExposesHostAndDomainParts()
    {
        var url = WebsiteUrl.Create("https://shop.Acme.co.uk/products").ShouldBeSuccess();

        url.ShouldSatisfyAllConditions(
            () => url.Host.ShouldBe("shop.acme.co.uk"),
            () => url.RegistrableDomain.ShouldBe("acme.co.uk"),
            () => url.DomainLabel.ShouldBe("acme"));
    }

    [TestCase("https://www.müller.de")]
    [TestCase("https://www.MÜLLER.de")]
    [TestCase("https://www.xn--mller-kva.de")]
    public void Create_WithInternationalizedHost_CanonicalizesToUnicode(string input)
    {
        var url = WebsiteUrl.Create(input).ShouldBeSuccess();

        url.ShouldSatisfyAllConditions(
            () => url.Value.ShouldBe("https://www.müller.de"),
            () => url.Host.ShouldBe("www.müller.de"),
            () => url.DomainLabel.ShouldBe("müller"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithMissingUrl_FailsAsRequired(string? input)
    {
        WebsiteUrl.Create(input).ShouldBeValidationError("website.required", "website");
    }

    [TestCase("not a url")]
    [TestCase("www.example.com")]
    [TestCase("/relative/path")]
    [TestCase("https://")]
    public void Create_WithMalformedUrl_Fails(string input)
    {
        WebsiteUrl.Create(input).ShouldBeValidationError("website.malformed", "website");
    }

    [TestCase("ftp://example.com")]
    [TestCase("file:///c:/temp/x.html")]
    [TestCase("mailto:info@example.com")]
    public void Create_WithUnsupportedScheme_Fails(string input)
    {
        WebsiteUrl.Create(input).ShouldBeValidationError("website.unsupported_scheme", "website");
    }

    [TestCase("https://localhost")]
    [TestCase("https://intranet")]
    [TestCase("https://co.uk")]
    [TestCase("https://192.168.1.10")]
    [TestCase("https://[::1]")]
    public void Create_WithoutPublicDomainName_Fails(string input)
    {
        WebsiteUrl.Create(input).ShouldBeValidationError("website.invalid_host", "website");
    }

    [Test]
    public void Create_WithCredentials_Fails()
    {
        WebsiteUrl.Create("https://user:pass@example.com")
            .ShouldBeValidationError("website.credentials_not_allowed", "website");
    }

    [Test]
    public void Create_TooLong_Fails()
    {
        var input = "https://example.com/" + new string('a', WebsiteUrl.MaxLength);

        WebsiteUrl.Create(input).ShouldBeValidationError("website.too_long", "website");
    }
}

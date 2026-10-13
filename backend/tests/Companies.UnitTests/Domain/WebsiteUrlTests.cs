using Companies.Domain.Common;
using Companies.Domain.Companies;

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
        var result = WebsiteUrl.Create(input);

        Assert.That(result.IsSuccess, Is.True, () => result.Error.Message);
        Assert.That(result.Value.Value, Is.EqualTo(expected));
    }

    [Test]
    public void Create_ExposesHostAndDomainParts()
    {
        var url = WebsiteUrl.Create("https://shop.Acme.co.uk/products").Value;

        Assert.Multiple(() =>
        {
            Assert.That(url.Host, Is.EqualTo("shop.acme.co.uk"));
            Assert.That(url.RegistrableDomain, Is.EqualTo("acme.co.uk"));
            Assert.That(url.DomainLabel, Is.EqualTo("acme"));
        });
    }

    [TestCase("https://www.müller.de")]
    [TestCase("https://www.MÜLLER.de")]
    [TestCase("https://www.xn--mller-kva.de")]
    public void Create_WithInternationalizedHost_CanonicalizesToUnicode(string input)
    {
        var url = WebsiteUrl.Create(input).Value;

        Assert.Multiple(() =>
        {
            Assert.That(url.Value, Is.EqualTo("https://www.müller.de"));
            Assert.That(url.Host, Is.EqualTo("www.müller.de"));
            Assert.That(url.DomainLabel, Is.EqualTo("müller"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithMissingUrl_FailsAsRequired(string? input)
    {
        AssertValidationError(WebsiteUrl.Create(input), "website.required");
    }

    [TestCase("not a url")]
    [TestCase("www.example.com")]
    [TestCase("/relative/path")]
    [TestCase("https://")]
    public void Create_WithMalformedUrl_Fails(string input)
    {
        AssertValidationError(WebsiteUrl.Create(input), "website.malformed");
    }

    [TestCase("ftp://example.com")]
    [TestCase("file:///c:/temp/x.html")]
    [TestCase("mailto:info@example.com")]
    public void Create_WithUnsupportedScheme_Fails(string input)
    {
        AssertValidationError(WebsiteUrl.Create(input), "website.unsupported_scheme");
    }

    [TestCase("https://localhost")]
    [TestCase("https://intranet")]
    [TestCase("https://co.uk")]
    [TestCase("https://192.168.1.10")]
    [TestCase("https://[::1]")]
    public void Create_WithoutPublicDomainName_Fails(string input)
    {
        AssertValidationError(WebsiteUrl.Create(input), "website.invalid_host");
    }

    [Test]
    public void Create_WithCredentials_Fails()
    {
        AssertValidationError(WebsiteUrl.Create("https://user:pass@example.com"), "website.credentials_not_allowed");
    }

    [Test]
    public void Create_TooLong_Fails()
    {
        var input = "https://example.com/" + new string('a', WebsiteUrl.MaxLength);

        AssertValidationError(WebsiteUrl.Create(input), "website.too_long");
    }

    private static void AssertValidationError(Result<WebsiteUrl> result, string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Target, Is.EqualTo("website"));
            Assert.That(result.Error.Message, Is.Not.Empty);
        });
    }
}

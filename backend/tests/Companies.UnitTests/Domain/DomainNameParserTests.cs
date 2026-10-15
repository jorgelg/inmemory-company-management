using Companies.Domain.Companies;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class DomainNameParserTests
{
    [TestCase("example.com", "example.com", "example", "com")]
    [TestCase("www.example.com", "example.com", "example", "com")]
    [TestCase("shop.eu.example.com", "example.com", "example", "com")]
    [TestCase("WWW.Example.COM", "example.com", "example", "com")]
    [TestCase("example.com.", "example.com", "example", "com")]
    [TestCase("www.acme.co.uk", "acme.co.uk", "acme", "co.uk")]
    [TestCase("store.acme.com.au", "acme.com.au", "acme", "com.au")]
    [TestCase("abc.xyz", "abc.xyz", "abc", "xyz")]
    // Country second levels beyond any hand-picked list (Public Suffix List, ICANN section).
    [TestCase("google.com.bo", "google.com.bo", "google", "com.bo")]
    [TestCase("www.google.com.bo", "google.com.bo", "google", "com.bo")]
    [TestCase("www.ministerio.gob.bo", "ministerio.gob.bo", "ministerio", "gob.bo")]
    [TestCase("www.acme.com.ec", "acme.com.ec", "acme", "com.ec")]
    [TestCase("shop.acme.co.ke", "acme.co.ke", "acme", "co.ke")]
    [TestCase("www.acme.ltd.uk", "acme.ltd.uk", "acme", "ltd.uk")]
    // Private section: each tenant of a shared host is its own registrable domain.
    [TestCase("acme.github.io", "acme.github.io", "acme", "github.io")]
    // Wildcard rule "*.ck" and its exception "!www.ck".
    [TestCase("shop.acme.ck", "shop.acme.ck", "shop", "acme.ck")]
    [TestCase("www.ck", "www.ck", "www", "ck")]
    // Unlisted TLD falls back to the default rule "*".
    [TestCase("acme.notarealtld", "acme.notarealtld", "acme", "notarealtld")]
    // Internationalized suffix.
    [TestCase("例子.公司.cn", "例子.公司.cn", "例子", "公司.cn")]
    public void TryParse_ValidHost_ExtractsRegistrableDomain(
        string host, string registrable, string label, string suffix)
    {
        DomainNameParser.TryParse(host, out var parts).ShouldBeTrue();

        parts.ShouldNotBeNull().ShouldSatisfyAllConditions(
            p => p.RegistrableDomain.ShouldBe(registrable),
            p => p.Label.ShouldBe(label),
            p => p.PublicSuffix.ShouldBe(suffix));
    }

    [Test]
    public void TryParse_KeepsNormalizedHost()
    {
        DomainNameParser.TryParse("WWW.Acme.co.uk.", out var parts).ShouldBeTrue();

        parts.ShouldNotBeNull().Host.ShouldBe("www.acme.co.uk");
    }

    [TestCase("")]
    [TestCase("localhost")]
    [TestCase("com")]
    [TestCase("co.uk")]
    [TestCase("com.bo")]
    [TestCase("gob.bo")]
    [TestCase("github.io")]
    [TestCase("acme.ck")]
    [TestCase("example..com")]
    [TestCase(".example.com")]
    public void TryParse_InvalidHost_ReturnsFalse(string host)
    {
        DomainNameParser.TryParse(host, out var parts).ShouldBeFalse();

        parts.ShouldBeNull();
    }
}

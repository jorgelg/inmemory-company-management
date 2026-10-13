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
    [TestCase("example..com")]
    [TestCase(".example.com")]
    public void TryParse_InvalidHost_ReturnsFalse(string host)
    {
        DomainNameParser.TryParse(host, out var parts).ShouldBeFalse();

        parts.ShouldBeNull();
    }
}

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
        var parsed = DomainNameParser.TryParse(host, out var parts);

        Assert.That(parsed, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(parts!.RegistrableDomain, Is.EqualTo(registrable));
            Assert.That(parts.Label, Is.EqualTo(label));
            Assert.That(parts.PublicSuffix, Is.EqualTo(suffix));
        });
    }

    [Test]
    public void TryParse_KeepsNormalizedHost()
    {
        var parsed = DomainNameParser.TryParse("WWW.Acme.co.uk.", out var parts);

        Assert.That(parsed, Is.True);
        Assert.That(parts!.Host, Is.EqualTo("www.acme.co.uk"));
    }

    [TestCase("")]
    [TestCase("localhost")]
    [TestCase("com")]
    [TestCase("co.uk")]
    [TestCase("example..com")]
    [TestCase(".example.com")]
    public void TryParse_InvalidHost_ReturnsFalse(string host)
    {
        var parsed = DomainNameParser.TryParse(host, out var parts);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(parts, Is.Null);
        });
    }
}

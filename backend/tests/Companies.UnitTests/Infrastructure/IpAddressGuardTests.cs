using System.Net;
using Companies.Infrastructure.WebContent;

namespace Companies.UnitTests.Infrastructure;

[TestFixture]
public sealed class IpAddressGuardTests
{
    [TestCase("127.0.0.1")]
    [TestCase("10.1.2.3")]
    [TestCase("172.16.0.1")]
    [TestCase("172.31.255.255")]
    [TestCase("192.168.1.10")]
    [TestCase("169.254.169.254")]
    [TestCase("100.64.0.1")]
    [TestCase("0.0.0.0")]
    [TestCase("224.0.0.1")]
    [TestCase("255.255.255.255")]
    [TestCase("::1")]
    [TestCase("::")]
    [TestCase("fe80::1")]
    [TestCase("fd00::1")]
    [TestCase("ff02::1")]
    [TestCase("::ffff:127.0.0.1")]
    [TestCase("::ffff:10.0.0.1")]
    public void IsBlocked_NonPublicAddress_ReturnsTrue(string address)
    {
        IpAddressGuard.IsBlocked(IPAddress.Parse(address)).ShouldBeTrue();
    }

    [TestCase("93.184.216.34")]
    [TestCase("8.8.8.8")]
    [TestCase("172.32.0.1")]
    [TestCase("100.128.0.1")]
    [TestCase("2606:2800:220:1:248:1893:25c8:1946")]
    [TestCase("::ffff:93.184.216.34")]
    public void IsBlocked_PublicAddress_ReturnsFalse(string address)
    {
        IpAddressGuard.IsBlocked(IPAddress.Parse(address)).ShouldBeFalse();
    }

    [Test]
    public async Task ResolvePublicAddresses_AllPublic_ReturnsThem()
    {
        IPAddress[] resolved = [IPAddress.Parse("93.184.216.34")];

        var addresses = await IpAddressGuard.ResolvePublicAddressesAsync(
            "example.com", (_, _) => Task.FromResult(resolved), CancellationToken.None);

        addresses.ShouldBe(resolved);
    }

    [Test]
    public async Task ResolvePublicAddresses_AnyBlocked_Throws()
    {
        IPAddress[] resolved = [IPAddress.Parse("93.184.216.34"), IPAddress.Parse("10.0.0.5")];

        var exception = await Should.ThrowAsync<HttpRequestException>(() => IpAddressGuard.ResolvePublicAddressesAsync(
            "rebind.example", (_, _) => Task.FromResult(resolved), CancellationToken.None));

        exception.Message.ShouldContain("non-public");
    }

    [Test]
    public async Task ResolvePublicAddresses_NoneResolved_Throws()
    {
        await Should.ThrowAsync<HttpRequestException>(() => IpAddressGuard.ResolvePublicAddressesAsync(
            "nowhere.example", (_, _) => Task.FromResult(Array.Empty<IPAddress>()), CancellationToken.None));
    }
}

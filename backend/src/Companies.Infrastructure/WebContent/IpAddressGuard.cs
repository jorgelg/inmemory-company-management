using System.Net;
using System.Net.Sockets;

namespace Companies.Infrastructure.WebContent;

/// <summary>
/// SSRF protection: the content fetcher may only connect to public internet addresses, never to
/// loopback, private, link-local (cloud metadata), carrier-grade NAT, multicast or reserved ranges.
/// </summary>
internal static class IpAddressGuard
{
    public static bool IsBlocked(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsBlockedIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsBlockedIPv6(address),
            _ => true,
        };
    }

    /// <summary>
    /// Resolves <paramref name="host"/> and returns its addresses only if every one is public.
    /// Connecting to exactly these addresses (instead of resolving again) defeats DNS rebinding.
    /// </summary>
    public static async Task<IPAddress[]> ResolvePublicAddressesAsync(
        string host,
        Func<string, CancellationToken, Task<IPAddress[]>> resolve,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolve);

        var addresses = await resolve(host, cancellationToken).ConfigureAwait(false);

        if (addresses.Length == 0)
        {
            throw new HttpRequestException($"Host '{host}' did not resolve to any address.");
        }

        if (addresses.Any(IsBlocked))
        {
            throw new HttpRequestException($"Host '{host}' resolves to a non-public address; the request was blocked.");
        }

        return addresses;
    }

    private static bool IsBlockedIPv4(byte[] b) => b[0] switch
    {
        0 or 10 or 127 => true,                       // "this" network, private, loopback
        100 => b[1] is >= 64 and <= 127,              // carrier-grade NAT 100.64.0.0/10
        169 => b[1] == 254,                           // link-local, incl. 169.254.169.254 metadata
        172 => b[1] is >= 16 and <= 31,               // private 172.16.0.0/12
        192 => b[1] == 168 || (b[1] == 0 && b[2] is 0 or 2), // private, IETF protocol, TEST-NET-1
        198 => b[1] is 18 or 19,                      // benchmarking 198.18.0.0/15
        >= 224 => true,                               // multicast, reserved, broadcast
        _ => false,
    };

    private static bool IsBlockedIPv6(IPAddress address)
    {
        if (address.Equals(IPAddress.IPv6Any)
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6UniqueLocal
            || address.IsIPv6Multicast)
        {
            return true;
        }

        var b = address.GetAddressBytes();
        return b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8; // documentation 2001:db8::/32
    }
}

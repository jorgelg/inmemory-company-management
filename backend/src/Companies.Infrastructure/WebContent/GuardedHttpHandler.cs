using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Companies.Infrastructure.WebContent;

/// <summary>
/// The primary handler for the content fetcher. Every connection, including each redirect hop,
/// goes through <see cref="ConnectAsync"/>, which only dials addresses that passed
/// <see cref="IpAddressGuard"/>. Redirects are followed by the fetcher, not the handler, so each
/// target is validated first.
/// </summary>
internal static class GuardedHttpHandler
{
    public static SocketsHttpHandler Create(WebsiteFetcherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = options.Timeout,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            UseCookies = false,
            UseProxy = false, // a proxy would make us dial the proxy's address, bypassing the guard
            ConnectCallback = ConnectAsync,
        };
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Ownership of the socket passes to the returned NetworkStream; it is disposed on failure.")]
    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var endpoint = context.DnsEndPoint;
        var addresses = await IpAddressGuard
            .ResolvePublicAddressesAsync(endpoint.Host, Dns.GetHostAddressesAsync, cancellationToken)
            .ConfigureAwait(false);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, endpoint.Port, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}

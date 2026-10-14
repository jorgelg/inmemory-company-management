using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Companies.Application.Relevance;
using Companies.Domain.Companies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Companies.Infrastructure.WebContent;

/// <summary>
/// Reads a company's home page for the relevance check. Never throws for network or content
/// problems: timeouts, error statuses, non-HTML responses, disallowed redirects and blocked
/// addresses all return <see langword="null"/> and log a warning. Only caller cancellation propagates.
/// </summary>
internal sealed class HttpWebsiteContentFetcher(
    HttpClient httpClient,
    IOptions<WebsiteFetcherOptions> options,
    ILogger<HttpWebsiteContentFetcher> logger) : IWebsiteContentFetcher
{
    public async Task<WebsiteContent?> FetchAsync(WebsiteUrl website, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(website);

        var settings = options.Value;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(settings.Timeout);

        var target = new Uri(website.Value);

        try
        {
            for (var redirects = 0; ; redirects++)
            {
                using var response = await SendAsync(target, budget.Token).ConfigureAwait(false);

                if (IsRedirect(response.StatusCode))
                {
                    if (redirects >= settings.MaxRedirects)
                    {
                        return Failed(website, $"more than {settings.MaxRedirects} redirects");
                    }

                    var next = ResolveRedirect(target, response.Headers.Location);
                    if (next is null)
                    {
                        return Failed(website, $"redirect to disallowed location '{response.Headers.Location}'");
                    }

                    target = next;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return Failed(website, $"HTTP {(int)response.StatusCode}");
                }

                if (!IsHtml(response.Content.Headers.ContentType))
                {
                    return Failed(website, $"non-HTML content '{response.Content.Headers.ContentType?.MediaType}'");
                }

                var html = await ReadPrefixAsync(response.Content, settings.MaxResponseBytes, budget.Token).ConfigureAwait(false);
                var content = HtmlMetadataParser.Parse(html);

                logger.ContentFetched(website.Host, content.Title);
                return content;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed(website, $"timed out after {settings.Timeout.TotalMilliseconds:0} ms");
        }
        catch (HttpRequestException exception)
        {
            return Failed(website, exception.Message);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri target, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");

        return await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }

    private WebsiteContent? Failed(WebsiteUrl website, string reason)
    {
        logger.ContentUnavailable(website.Host, reason);
        return null;
    }

    private static bool IsRedirect(HttpStatusCode status) => status is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
        or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    /// <summary>A redirect target must itself be a valid public http(s) website URL.</summary>
    private static Uri? ResolveRedirect(Uri current, Uri? location)
    {
        if (location is null)
        {
            return null;
        }

        // On Unix "/path" parses as an absolute file URI; treat it as the relative path it is.
        if (location.IsAbsoluteUri && location.IsFile && location.OriginalString.StartsWith('/'))
        {
            location = new Uri(location.OriginalString, UriKind.Relative);
        }

        var absolute = location.IsAbsoluteUri ? location : new Uri(current, location);
        var website = WebsiteUrl.Create(absolute.AbsoluteUri);

        return website.IsSuccess ? new Uri(website.Value.Value) : null;
    }

    private static bool IsHtml(MediaTypeHeaderValue? contentType) =>
        contentType?.MediaType is null or "text/html" or "application/xhtml+xml";

    private static async Task<string> ReadPrefixAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var total = 0;
                int read;
                while (total < maxBytes
                       && (read = await stream.ReadAsync(buffer.AsMemory(total, maxBytes - total), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                }

                return EncodingOf(content.Headers.ContentType).GetString(buffer, 0, total);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static Encoding EncodingOf(MediaTypeHeaderValue? contentType)
    {
        try
        {
            return contentType?.CharSet is { Length: > 0 } charset
                ? Encoding.GetEncoding(charset.Trim('"'))
                : Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}

internal static partial class HttpWebsiteContentFetcherLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Read website content from {Host} (title '{Title}')")]
    public static partial void ContentFetched(this ILogger logger, string host, string? title);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read website content from {Host}: {Reason}")]
    public static partial void ContentUnavailable(this ILogger logger, string host, string reason);
}

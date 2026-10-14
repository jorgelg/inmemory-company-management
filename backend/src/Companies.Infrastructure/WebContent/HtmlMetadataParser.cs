using System.Net;
using System.Text.RegularExpressions;
using Companies.Application.Relevance;

namespace Companies.Infrastructure.WebContent;

/// <summary>
/// Pulls the identifying text out of an HTML page: <c>&lt;title&gt;</c>, <c>og:site_name</c> and the
/// first <c>&lt;h1&gt;</c>. Deliberately small: no DOM, bounded input, regex timeouts.
/// </summary>
internal static partial class HtmlMetadataParser
{
    public const int MaxValueLength = 200;

    private const int RegexTimeoutMs = 250;

    public static WebsiteContent Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        return new WebsiteContent(
            Title: Extract(TitleElement(), html),
            SiteName: Extract(OpenGraphSiteName(), html),
            Heading: Extract(FirstHeading(), html));
    }

    private static string? Extract(Regex pattern, string html)
    {
        try
        {
            var match = pattern.Match(html);
            if (!match.Success)
            {
                return null;
            }

            var text = WebUtility.HtmlDecode(InnerTag().Replace(match.Groups["value"].Value, " "));
            text = Whitespace().Replace(text, " ").Trim();

            return text.Length == 0 ? null : text[..Math.Min(text.Length, MaxValueLength)];
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"<title\b[^>]*>(?<value>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMs)]
    private static partial Regex TitleElement();

    // The lookahead finds property="og:site_name" anywhere in the tag, so attribute order doesn't matter.
    [GeneratedRegex(
        @"<meta\b(?=[^>]*\bproperty\s*=\s*[""']og:site_name[""'])[^>]*\bcontent\s*=\s*(?<quote>[""'])(?<value>.*?)\k<quote>[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMs)]
    private static partial Regex OpenGraphSiteName();

    [GeneratedRegex(@"<h1\b[^>]*>(?<value>.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMs)]
    private static partial Regex FirstHeading();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.None, RegexTimeoutMs)]
    private static partial Regex InnerTag();

    [GeneratedRegex(@"\s+", RegexOptions.None, RegexTimeoutMs)]
    private static partial Regex Whitespace();
}

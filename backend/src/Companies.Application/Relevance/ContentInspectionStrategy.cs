using Companies.Application.Text;
using Microsoft.Extensions.Options;

namespace Companies.Application.Relevance;

/// <summary>
/// Fallback for brands whose domain doesn't spell their name (Alphabet ↔ abc.xyz): fetches the
/// home page and looks for the name in its title, <c>og:site_name</c> and first heading.
/// <list type="table">
///   <item><term>0.75</term><description>the content contains the whole name</description></item>
///   <item><term>0.00–0.60</term><description>the content contains some of the words, proportionally</description></item>
/// </list>
/// </summary>
internal sealed class ContentInspectionStrategy(IWebsiteContentFetcher fetcher, IOptions<RelevanceOptions> options)
    : IRelevanceStrategy
{
    public const string StrategyName = "content-inspection";

    private const int MinNameLength = 3;

    public string Name => StrategyName;

    public async Task<RelevanceEvidence> EvaluateAsync(RelevanceCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!options.Value.ContentInspection.Enabled)
        {
            return Evidence(0, "website content inspection is disabled");
        }

        var content = await fetcher.FetchAsync(candidate.Website, cancellationToken).ConfigureAwait(false);
        if (content is null)
        {
            return Evidence(0, "website content could not be retrieved");
        }

        var text = string.Join(' ', new[] { content.Title, content.SiteName, content.Heading }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        var name = candidate.NormalizedName;

        if (name.Compact.Length >= MinNameLength
            && TextNormalizer.FoldCompact(text).Contains(name.Compact, StringComparison.Ordinal))
        {
            return Evidence(0.75, $"the website's title or headings mention '{name.Compact}'");
        }

        var contentWords = TextNormalizer.Words(text).ToHashSet(StringComparer.Ordinal);
        var matched = name.SignificantWords.Count(contentWords.Contains);

        return matched > 0
            ? Evidence(0.6 * matched / name.SignificantWords.Count,
                $"the website's title or headings mention {matched} of {name.SignificantWords.Count} words of the name")
            : Evidence(0, "the website's title and headings do not mention the name");
    }

    private static RelevanceEvidence Evidence(double score, string reason) => new(StrategyName, score, reason);
}

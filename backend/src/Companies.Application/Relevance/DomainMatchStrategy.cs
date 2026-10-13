using System.Globalization;
using Companies.Application.Text;

namespace Companies.Application.Relevance;

/// <summary>
/// Compares the normalized company name with the website's registrable domain label, offline.
/// Several signals are computed and the strongest wins:
/// <list type="table">
///   <item><term>1.00</term><description>label equals the name (verus.com ↔ "Verus LLC")</description></item>
///   <item><term>0.90</term><description>label contains every word (smithandjoneslaw ↔ "Smith &amp; Jones Law")</description></item>
///   <item><term>0.85</term><description>label and name contain each other (acmecorp ↔ "Acme")</description></item>
///   <item><term>0.80</term><description>label is the acronym (ibm ↔ "International Business Machines")</description></item>
///   <item><term>0.30–0.80</term><description>label contains some of the words, proportionally</description></item>
///   <item><term>0.00–0.80</term><description>Jaro-Winkler similarity above 0.7, for typos</description></item>
/// </list>
/// </summary>
internal sealed class DomainMatchStrategy : IRelevanceStrategy
{
    public const string StrategyName = "domain-match";

    private const int MinOverlapLength = 3;
    private const double MinSimilarity = 0.7;

    public string Name => StrategyName;

    public Task<RelevanceEvidence> EvaluateAsync(RelevanceCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return Task.FromResult(Evaluate(candidate.NormalizedName, TextNormalizer.FoldCompact(candidate.Website.DomainLabel)));
    }

    private static RelevanceEvidence Evaluate(NormalizedName name, string label) =>
        Signals(name, label).MaxBy(evidence => evidence.Score)!;

    private static IEnumerable<RelevanceEvidence> Signals(NormalizedName name, string label)
    {
        var words = name.SignificantWords;

        if (label == name.Compact || label == name.FullCompact)
        {
            yield return Evidence(1.0, $"domain label '{label}' matches the name exactly");
        }

        if (words.Count > 1 && words.All(word => label.Contains(word, StringComparison.Ordinal)))
        {
            yield return Evidence(0.9, $"domain label '{label}' contains every word of the name");
        }

        if (Overlaps(label, name.Compact))
        {
            yield return Evidence(0.85, $"the name and domain label '{label}' overlap");
        }

        if (name.Acronyms.Contains(label, StringComparer.Ordinal))
        {
            yield return Evidence(0.8, $"domain label '{label}' is the acronym of the name");
        }

        var matched = words.Count(word => word.Length >= MinOverlapLength && label.Contains(word, StringComparison.Ordinal));
        if (matched > 0 && matched < words.Count)
        {
            var fraction = (double)matched / words.Count;
            yield return Evidence(0.3 + (0.5 * fraction), $"domain label '{label}' contains {matched} of {words.Count} words of the name");
        }

        var similarity = StringSimilarity.JaroWinkler(name.Compact, label);
        if (similarity >= MinSimilarity)
        {
            var score = (similarity - MinSimilarity) / (1 - MinSimilarity) * 0.8;
            yield return Evidence(score, string.Create(
                CultureInfo.InvariantCulture, $"domain label '{label}' is similar to the name (similarity {similarity:0.00})"));
        }

        yield return Evidence(0, $"domain label '{label}' does not resemble the name");
    }

    private static bool Overlaps(string label, string compact) =>
        label != compact
        && Math.Min(label.Length, compact.Length) >= MinOverlapLength
        && (label.Contains(compact, StringComparison.Ordinal) || compact.Contains(label, StringComparison.Ordinal));

    private static RelevanceEvidence Evidence(double score, string reason) => new(StrategyName, score, reason);
}

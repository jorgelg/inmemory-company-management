using Companies.Application.Text;
using Companies.Domain.Companies;

namespace Companies.Application.Search;

internal sealed record ScoredCompany(Company Company, double Score);

internal interface ICompanySearchRanker
{
    /// <summary>Scores each company against <paramref name="term"/>, drops non-matches, best first.</summary>
    IReadOnlyList<ScoredCompany> Rank(IEnumerable<Company> companies, string term);
}

/// <summary>
/// Explainable relevance ranking for free-text search. The highest matching rule wins:
/// <list type="table">
///   <item><term>1.0</term><description>name equals the term (ignoring case, accents and legal suffixes)</description></item>
///   <item><term>0.8</term><description>name starts with the term</description></item>
///   <item><term>0.7</term><description>a word of the name starts with the term</description></item>
///   <item><term>0.6</term><description>every word of a multi-word term starts a word of the name</description></item>
///   <item><term>0.5</term><description>name contains the term</description></item>
///   <item><term>0.4</term><description>domain label contains the term</description></item>
///   <item><term>0.3</term><description>a word of the name is a near match (Jaro-Winkler ≥ 0.85)</description></item>
/// </list>
/// Ties are ordered by name.
/// </summary>
internal sealed class CompanySearchRanker : ICompanySearchRanker
{
    private const double FuzzyThreshold = 0.85;
    private const int MinDomainTermLength = 2;

    public IReadOnlyList<ScoredCompany> Rank(IEnumerable<Company> companies, string term)
    {
        ArgumentNullException.ThrowIfNull(companies);

        var query = new SearchTerm(term);
        if (query.Folded.Length == 0)
        {
            return [];
        }

        return
        [
            .. companies
                .Select(company => new ScoredCompany(company, Score(company, query)))
                .Where(scored => scored.Score > 0)
                .OrderByDescending(scored => scored.Score)
                .ThenBy(scored => scored.Company.Name.Value, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static double Score(Company company, SearchTerm term)
    {
        var name = TextNormalizer.Fold(company.Name.Value);
        var nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (name == term.Folded || CompanyNameNormalizer.Normalize(company.Name).Compact == term.Compact)
        {
            return 1.0;
        }

        if (name.StartsWith(term.Folded, StringComparison.Ordinal))
        {
            return 0.8;
        }

        if (term.Words.Length == 1 && nameWords.Any(word => word.StartsWith(term.Folded, StringComparison.Ordinal)))
        {
            return 0.7;
        }

        if (term.Words.Length > 1
            && term.Words.All(t => nameWords.Any(word => word.StartsWith(t, StringComparison.Ordinal))))
        {
            return 0.6;
        }

        if (name.Contains(term.Folded, StringComparison.Ordinal))
        {
            return 0.5;
        }

        if (term.Compact.Length >= MinDomainTermLength
            && TextNormalizer.FoldCompact(company.Website.DomainLabel).Contains(term.Compact, StringComparison.Ordinal))
        {
            return 0.4;
        }

        if (nameWords.Any(word => StringSimilarity.JaroWinkler(word, term.Compact) >= FuzzyThreshold))
        {
            return 0.3;
        }

        return 0;
    }

    private sealed class SearchTerm(string raw)
    {
        public string Folded { get; } = TextNormalizer.Fold(raw);

        public string[] Words => Folded.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        public string Compact => Folded.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}

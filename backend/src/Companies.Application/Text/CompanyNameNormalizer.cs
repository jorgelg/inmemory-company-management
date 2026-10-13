using System.Collections.Frozen;
using Companies.Domain.Companies;

namespace Companies.Application.Text;

/// <summary>
/// Normalizes a company name for matching: folds it (<see cref="TextNormalizer"/>), strips trailing
/// legal-form suffixes (LLC, Inc, GmbH, S.A., …) and drops generic words (the, and, group, …).
/// </summary>
public static class CompanyNameNormalizer
{
    private static readonly FrozenSet<string> LegalSuffixes = new[]
    {
        "llc", "lc", "pllc", "llp", "lp", "inc", "incorporated", "corp", "corporation", "co", "company",
        "ltd", "limited", "plc", "pty", "pvt", "gmbh", "ag", "kg", "se", "sa", "sas", "sarl", "srl", "spa",
        "sl", "sro", "bv", "nv", "oy", "ab", "kk",
    }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> StopWords = new[]
    {
        "the", "and", "of", "for", "de", "la", "group", "holding", "holdings",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static NormalizedName Normalize(CompanyName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var folded = TextNormalizer.Words(name.Value);

        var end = folded.Length;
        while (end > 0 && LegalSuffixes.Contains(folded[end - 1]))
        {
            end--;
        }

        string[] words = folded[..end];
        string[] significant = [.. words.Where(word => !StopWords.Contains(word))];

        // A name made only of generic words ("The Company LLC") is still matched on what it says.
        if (significant.Length == 0)
        {
            words = folded;
            significant = folded;
        }

        return new NormalizedName(words, significant, Acronyms(words, significant));
    }

    private static string[] Acronyms(string[] words, string[] significant) =>
        [.. new[] { significant, words }
            .Where(list => list.Length > 1)
            .Select(list => string.Concat(list.Select(word => word[0])))
            .Distinct(StringComparer.Ordinal)];
}

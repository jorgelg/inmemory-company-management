using System.Collections.Frozen;

namespace Companies.Domain.Companies;

/// <summary>
/// The Mozilla Public Suffix List (https://publicsuffix.org), ICANN and private sections, read from
/// an embedded snapshot (<c>public_suffix_list.dat</c>, its <c>// VERSION:</c> line gives the date).
/// Offline and deterministic. Refresh by replacing the file with the latest list.
/// </summary>
internal static class PublicSuffixList
{
    private const string ResourceName = "Companies.Domain.public_suffix_list.dat";

    private static readonly Lazy<Rules> Loaded = new(Load);

    /// <summary>Snapshot version from the list's header, e.g. <c>2026-10-07_07-28-19_UTC</c>.</summary>
    public static string Version => Loaded.Value.Version;

    /// <summary>
    /// How many trailing labels of <paramref name="labels"/> form the public suffix, using the
    /// standard algorithm: an exception rule wins; otherwise the longest matching normal or
    /// wildcard rule; otherwise the default rule "*" (one label).
    /// </summary>
    public static int SuffixLabelCount(IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        var rules = Loaded.Value;
        var longest = 1;

        for (var count = labels.Count; count >= 1; count--)
        {
            var candidate = string.Join('.', labels.Skip(labels.Count - count));

            if (rules.Exceptions.Contains(candidate))
            {
                // "!www.ck": the suffix is the exception minus its leftmost label.
                return count - 1;
            }

            if (count > longest && (rules.Normal.Contains(candidate) || MatchesWildcard(rules, labels, count)))
            {
                longest = count;
            }
        }

        return longest;
    }

    private static bool MatchesWildcard(Rules rules, IReadOnlyList<string> labels, int count) =>
        count >= 2 && rules.Wildcards.Contains(string.Join('.', labels.Skip(labels.Count - count + 1)));

    private static Rules Load()
    {
        using var stream = typeof(PublicSuffixList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        var normal = new HashSet<string>(StringComparer.Ordinal);
        var wildcards = new HashSet<string>(StringComparer.Ordinal);
        var exceptions = new HashSet<string>(StringComparer.Ordinal);
        var version = "unknown";

        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("// VERSION:", StringComparison.Ordinal))
            {
                version = trimmed["// VERSION:".Length..].Trim();
                continue;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            // A rule is the first whitespace-delimited token; entries are already lower-case Unicode.
            var rule = trimmed.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

            if (rule.StartsWith('!'))
            {
                exceptions.Add(rule[1..]);
            }
            else if (rule.StartsWith("*.", StringComparison.Ordinal))
            {
                wildcards.Add(rule[2..]);
            }
            else
            {
                normal.Add(rule);
            }
        }

        return new Rules(normal.ToFrozenSet(StringComparer.Ordinal), wildcards.ToFrozenSet(StringComparer.Ordinal),
            exceptions.ToFrozenSet(StringComparer.Ordinal), version);
    }

    private sealed record Rules(
        FrozenSet<string> Normal,
        FrozenSet<string> Wildcards,
        FrozenSet<string> Exceptions,
        string Version);
}

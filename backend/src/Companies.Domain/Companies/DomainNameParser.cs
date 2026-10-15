using System.Diagnostics.CodeAnalysis;

namespace Companies.Domain.Companies;

/// <summary>
/// Splits a host name into its registrable domain and label using the Public Suffix List
/// (<see cref="PublicSuffixList"/>): <c>www.google.com.bo</c> → <c>google.com.bo</c> / <c>google</c>,
/// <c>acme.github.io</c> → <c>acme.github.io</c> / <c>acme</c>. A host that is itself a public
/// suffix (<c>com.bo</c>, <c>co.uk</c>, <c>github.io</c>) has no registrable domain.
/// </summary>
public static class DomainNameParser
{
    public static bool TryParse(string? host, [NotNullWhen(true)] out DomainParts? parts)
    {
        parts = null;

        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        var labels = normalized.Split('.');

        if (labels.Length < 2 || labels.Any(string.IsNullOrEmpty))
        {
            return false;
        }

        var suffixLabelCount = PublicSuffixList.SuffixLabelCount(labels);

        if (labels.Length <= suffixLabelCount)
        {
            return false;
        }

        var suffix = string.Join('.', labels[^suffixLabelCount..]);
        var label = labels[^(suffixLabelCount + 1)];

        parts = new DomainParts(normalized, $"{label}.{suffix}", label, suffix);
        return true;
    }
}

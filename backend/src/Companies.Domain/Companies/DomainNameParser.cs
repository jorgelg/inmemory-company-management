using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Companies.Domain.Companies;

/// <summary>The pieces of a host name that matter for matching it against a company name.</summary>
/// <param name="Host">Lower-cased host without a trailing dot, e.g. <c>www.acme.co.uk</c>.</param>
/// <param name="RegistrableDomain">The domain a company actually registers, e.g. <c>acme.co.uk</c>.</param>
/// <param name="Label">The registrable label without its suffix, e.g. <c>acme</c>.</param>
/// <param name="PublicSuffix">The suffix under which domains are registered, e.g. <c>co.uk</c>.</param>
public sealed record DomainParts(string Host, string RegistrableDomain, string Label, string PublicSuffix);

/// <summary>
/// Splits a host name into its registrable domain and label. Handles single-label TLDs and a
/// curated list of common two-label public suffixes (co.uk, com.au, …). A full Public Suffix
/// List library can replace this without changing callers.
/// </summary>
public static class DomainNameParser
{
    private static readonly FrozenSet<string> MultiLabelSuffixes = new[]
    {
        "co.uk", "org.uk", "me.uk", "ac.uk", "gov.uk", "ltd.uk", "plc.uk",
        "com.au", "net.au", "org.au", "edu.au", "gov.au",
        "co.nz", "org.nz", "net.nz",
        "co.jp", "ne.jp", "or.jp",
        "co.kr", "or.kr",
        "co.in", "net.in", "org.in",
        "co.za", "org.za",
        "co.il", "org.il",
        "com.br", "net.br", "org.br",
        "com.mx", "com.ar", "com.co", "com.pe", "com.uy", "com.ve",
        "com.cn", "net.cn", "org.cn", "com.hk", "com.tw", "com.sg", "com.my", "com.ph",
        "com.tr", "com.eg", "com.sa",
    }.ToFrozenSet(StringComparer.Ordinal);

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

        var suffixLabelCount = MultiLabelSuffixes.Contains($"{labels[^2]}.{labels[^1]}") ? 2 : 1;

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

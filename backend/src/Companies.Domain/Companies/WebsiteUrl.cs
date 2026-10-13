using System.Globalization;
using System.Text.RegularExpressions;
using Companies.Domain.Common;

namespace Companies.Domain.Companies;

/// <summary>
/// An absolute http(s) URL whose host is a public domain name. Stored in canonical form:
/// lower-case scheme and host (Unicode, so IDN and punycode spellings compare equal),
/// no default port, no root slash, no fragment.
/// </summary>
public sealed partial record WebsiteUrl
{
    public const int MaxLength = 2048;

    private static readonly IdnMapping Idn = new();

    private WebsiteUrl(string value, DomainParts domain)
    {
        Value = value;
        Domain = domain;
    }

    public string Value { get; }

    private DomainParts Domain { get; }

    /// <summary>Full host, e.g. <c>shop.acme.co.uk</c>.</summary>
    public string Host => Domain.Host;

    /// <summary>Registrable domain, e.g. <c>acme.co.uk</c>. Used for duplicate detection and filtering.</summary>
    public string RegistrableDomain => Domain.RegistrableDomain;

    /// <summary>Registrable label, e.g. <c>acme</c>. Used for relevance matching.</summary>
    public string DomainLabel => Domain.Label;

    public static Result<WebsiteUrl> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CompanyErrors.WebsiteRequired;
        }

        var trimmed = input.Trim();

        if (trimmed.Length > MaxLength)
        {
            return CompanyErrors.WebsiteTooLong;
        }

        // Require an explicit scheme so OS-specific parsing (e.g. "/path" as a Unix file URI) never applies.
        if (!SchemePrefix().IsMatch(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return CompanyErrors.WebsiteMalformed;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return CompanyErrors.WebsiteUnsupportedScheme;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return CompanyErrors.WebsiteCredentialsNotAllowed;
        }

        if (uri.HostNameType != UriHostNameType.Dns || !TryGetUnicodeHost(uri, out var host)
            || !DomainNameParser.TryParse(host, out var domain))
        {
            return CompanyErrors.WebsiteInvalidHost;
        }

        return new WebsiteUrl(BuildCanonical(uri, domain.Host), domain);
    }

    public override string ToString() => Value;

    private static bool TryGetUnicodeHost(Uri uri, out string host)
    {
        try
        {
            host = Idn.GetUnicode(uri.IdnHost).ToLowerInvariant();
            return true;
        }
        catch (ArgumentException)
        {
            host = string.Empty;
            return false;
        }
    }

    private static string BuildCanonical(Uri uri, string host)
    {
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        var path = uri.AbsolutePath == "/" ? string.Empty : uri.AbsolutePath;

        return $"{uri.Scheme}://{host}{port}{path}{uri.Query}";
    }

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9+.-]*:")]
    private static partial Regex SchemePrefix();
}

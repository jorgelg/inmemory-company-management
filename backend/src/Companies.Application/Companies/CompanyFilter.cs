namespace Companies.Application.Companies;

/// <summary>
/// Criteria a store can apply directly (they translate to a SQL <c>WHERE</c>). All set criteria must match.
/// </summary>
/// <param name="NameContains">Case-insensitive substring of the company name.</param>
/// <param name="RegistrableDomain">Exact registrable domain, e.g. <c>acme.co.uk</c>.</param>
/// <param name="DomainContains">Case-insensitive substring of the host.</param>
public sealed record CompanyFilter(string? NameContains = null, string? RegistrableDomain = null, string? DomainContains = null)
{
    public static CompanyFilter None { get; } = new();
}

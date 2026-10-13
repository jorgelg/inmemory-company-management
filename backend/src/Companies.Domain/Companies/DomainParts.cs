namespace Companies.Domain.Companies;

/// <summary>The pieces of a host name that matter for matching it against a company name.</summary>
/// <param name="Host">Lower-cased host without a trailing dot, e.g. <c>www.acme.co.uk</c>.</param>
/// <param name="RegistrableDomain">The domain a company actually registers, e.g. <c>acme.co.uk</c>.</param>
/// <param name="Label">The registrable label without its suffix, e.g. <c>acme</c>.</param>
/// <param name="PublicSuffix">The suffix under which domains are registered, e.g. <c>co.uk</c>.</param>
public sealed record DomainParts(string Host, string RegistrableDomain, string Label, string PublicSuffix);

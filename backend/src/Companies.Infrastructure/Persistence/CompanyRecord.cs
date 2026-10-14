using Companies.Domain.Companies;

namespace Companies.Infrastructure.Persistence;

/// <summary>
/// The stored shape of a company: plain primitives, like a database row. Never leaves this layer;
/// the repository maps it to and from the <see cref="Company"/> entity.
/// </summary>
internal sealed record CompanyRecord(
    Guid Id,
    string Name,
    string Website,
    string Host,
    string RegistrableDomain,
    DateTimeOffset CreatedAt)
{
    public static CompanyRecord From(Company company) => new(
        company.Id,
        company.Name.Value,
        company.Website.Value,
        company.Website.Host,
        company.Website.RegistrableDomain,
        company.CreatedAt);

    /// <summary>Rebuilds the entity. Stored values were validated on the way in, so failure here is a bug.</summary>
    public Company ToCompany() => Company.Restore(
        Id,
        CompanyName.Create(Name).Value,
        WebsiteUrl.Create(Website).Value,
        CreatedAt);
}

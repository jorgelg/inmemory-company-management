using Companies.Application.Companies.Contracts;
using Companies.Domain.Companies;

namespace Companies.Application.Companies;

internal static class CompanyMappings
{
    public static CompanyResponse ToResponse(this Company company, double? relevanceScore = null) =>
        new(company.Id, company.Name.Value, company.Website.Value, company.Website.RegistrableDomain, company.CreatedAt)
        {
            RelevanceScore = relevanceScore is { } score ? Math.Round(score, 2) : null,
        };
}

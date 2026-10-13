using Companies.Domain.Companies;

namespace Companies.Application.Relevance;

/// <summary>Decides whether a company name is relevant to its website URL.</summary>
public interface IRelevanceValidator
{
    Task<RelevanceVerdict> EvaluateAsync(CompanyName name, WebsiteUrl website, CancellationToken cancellationToken);
}

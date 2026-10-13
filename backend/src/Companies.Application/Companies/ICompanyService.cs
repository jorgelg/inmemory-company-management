using Companies.Application.Companies.Contracts;
using Companies.Domain.Common;

namespace Companies.Application.Companies;

/// <summary>Company use cases. Expected failures come back as <see cref="Error"/>s, never as exceptions.</summary>
public interface ICompanyService
{
    /// <summary>Validates (format, uniqueness, name ↔ website relevance) and stores a company.</summary>
    Task<Result<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken);

    Task<Result<CompanyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Filters, searches, sorts and pages the stored companies. An empty page is a success.</summary>
    Task<Result<PagedResponse<CompanyResponse>>> SearchAsync(CompanyQuery query, CancellationToken cancellationToken);
}

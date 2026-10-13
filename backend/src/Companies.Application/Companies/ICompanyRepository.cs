using Companies.Domain.Companies;

namespace Companies.Application.Companies;

/// <summary>Persistence port for companies. Implementations live in Infrastructure.</summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Stores the company unless one with the same registrable domain already exists; returns
    /// <see langword="false"/> in that case. Must be atomic (a unique index in a database).
    /// </summary>
    Task<bool> TryAddAsync(Company company, CancellationToken cancellationToken);

    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByDomainAsync(string registrableDomain, CancellationToken cancellationToken);

    Task<IReadOnlyList<Company>> ListAsync(CompanyFilter filter, CancellationToken cancellationToken);
}

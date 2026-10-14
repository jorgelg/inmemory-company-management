using System.Collections.Concurrent;
using Companies.Application.Companies;
using Companies.Domain.Companies;

namespace Companies.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory store, registered as a singleton. Rows are kept by id, with a second
/// index on registrable domain that plays the role of a unique database index.
/// </summary>
internal sealed class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly ConcurrentDictionary<Guid, CompanyRecord> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _idByDomain = new(StringComparer.Ordinal);

    public Task<bool> TryAddAsync(Company company, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);
        cancellationToken.ThrowIfCancellationRequested();

        var record = CompanyRecord.From(company);

        // Claiming the domain first makes the uniqueness check and the insert one atomic step.
        if (!_idByDomain.TryAdd(record.RegistrableDomain, record.Id))
        {
            return Task.FromResult(false);
        }

        if (!_byId.TryAdd(record.Id, record))
        {
            _idByDomain.TryRemove(new KeyValuePair<string, Guid>(record.RegistrableDomain, record.Id));
            throw new InvalidOperationException($"A company with id {record.Id} is already stored.");
        }

        return Task.FromResult(true);
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_byId.TryGetValue(id, out var record) ? record.ToCompany() : null);
    }

    public Task<bool> ExistsByDomainAsync(string registrableDomain, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registrableDomain);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_idByDomain.ContainsKey(registrableDomain));
    }

    public Task<IReadOnlyList<Company>> ListAsync(CompanyFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Company> matches =
        [
            .. _byId.Values
                .Where(record => Matches(record, filter))
                .Select(record => record.ToCompany()),
        ];

        return Task.FromResult(matches);
    }

    private static bool Matches(CompanyRecord record, CompanyFilter filter) =>
        (filter.NameContains is null || record.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase))
        && (filter.RegistrableDomain is null || record.RegistrableDomain.Equals(filter.RegistrableDomain, StringComparison.OrdinalIgnoreCase))
        && (filter.DomainContains is null || record.Host.Contains(filter.DomainContains, StringComparison.OrdinalIgnoreCase));
}

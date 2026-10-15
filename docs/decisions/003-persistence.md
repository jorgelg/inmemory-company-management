# ADR 003: In-memory persistence

- **Status:** Accepted
- **Date:** 2026-10-14

## Context

The spec asks to:
- store validated companies **in memory only**;
- use **no database**;
- design the persistence layer so **a database can be introduced later with minimal changes**;
- keep **internal storage structures** from being exposed outside the application layer.

## Decision

**A port in Application, an in-memory adapter in Infrastructure.**

```csharp
public interface ICompanyRepository                                 // Companies.Application
{
    Task<bool> TryAddAsync(Company company, CancellationToken ct);  // false if the registrable domain is taken
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByDomainAsync(string registrableDomain, CancellationToken ct);
    Task<IReadOnlyList<Company>> ListAsync(CompanyFilter filter, CancellationToken ct);
}
```

- **Async with `CancellationToken` everywhere**, although the in-memory version completes synchronously. The signatures already fit a database driver, so callers never change.
- **`TryAddAsync` carries the uniqueness rule atomically.** That is the contract a unique index gives a database. The service does a cheap `ExistsByDomainAsync` pre-check to avoid a needless home-page fetch, but `TryAddAsync` is authoritative, so concurrent duplicates still get a 409.
- **`CompanyFilter`** contains only criteria a store can push down (name substring, exact registrable domain, host substring), which is what a SQL `WHERE` clause would hold. Relevance ranking, sorting and paging run in the application over the filtered set (see Consequences).
- **`InMemoryCompanyRepository`** (singleton):
  - a `ConcurrentDictionary<Guid, CompanyRecord>` holds the rows;
  - a `ConcurrentDictionary<string, Guid>` domain index plays the unique index. Claiming the domain first makes check-and-insert one atomic step: 50 parallel inserts of one domain give exactly one success, as a unit test shows.
- **`internal sealed record CompanyRecord`** stores primitives, like a database row, and maps to and from the `Company` entity. Every read rebuilds a fresh entity through `Company.Restore`, so no caller ever holds a reference into the store. An architecture test asserts that Infrastructure exports nothing but its `DependencyInjection` entry point.

## Moving to a database

1. Add `EfCompanyRepository : ICompanyRepository` (or Dapper) in Infrastructure, with a `DbContext`, a `CompanyRecord`-like entity configuration and a **unique index on `RegistrableDomain`**. `TryAddAsync` catches the unique-violation exception and returns `false`.
2. Change one line in `AddInfrastructure`:
   ```csharp
   services.AddScoped<ICompanyRepository, EfCompanyRepository>();   // was AddSingleton<…, InMemoryCompanyRepository>
   ```
3. For scale, push sort and paging into the repository for the non-search path (`ORDER BY … OFFSET/FETCH`, or keyset paging on the time-ordered Guid v7 ids), and serve `q` from a full-text index instead of the in-process ranker.

Domain, Application, Api and the frontend don't change.

## Consequences

- The spec's constraints are met literally: no database, nothing shared outside the layer, and a one-line swap.
- Data is lost on restart, which is expected for an in-memory store.
- Ranking and paging happen in memory after filtering. That is fine for an in-memory store; with a database it becomes an O(n) load for `q` searches, hence step 3 above.
- `ListAsync` rebuilds value objects on every read. That's negligible here, and it keeps the store's internals out of reach.

## Alternatives considered

- **EF Core with the InMemory provider.** Rejected, for three reasons:
  - Microsoft discourages it, even for testing, because it isn't a relational database. It has no transactions, no constraints such as unique indexes, and different string comparison and query translation, so code that works on it can fail on SQL Server or Postgres.
  - It blurs "do not use a database": the solution would already depend on an ORM and a provider.
  - It adds packages and a `DbContext` with no benefit while storage stays in memory.
- **SQLite in-memory.** A real database engine, so it contradicts the spec.
- **Storing `Company` entities directly in the dictionary.** Simpler, but it would hand out shared references to stored objects and skip the row/entity mapping a database adapter needs anyway.

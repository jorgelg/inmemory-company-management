namespace Companies.Domain.Companies;

/// <summary>
/// A registered company. Only constructible from already-validated value objects, so an
/// instance is always valid.
/// </summary>
public sealed class Company
{
    private Company(Guid id, CompanyName name, WebsiteUrl website, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Website = website;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public CompanyName Name { get; }

    public WebsiteUrl Website { get; }

    public DateTimeOffset CreatedAt { get; }

    /// <summary>Creates a new company with a time-ordered (version 7) identifier.</summary>
    public static Company Create(CompanyName name, WebsiteUrl website, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow();
        return new Company(Guid.CreateVersion7(now), name, website, now);
    }

    /// <summary>Rebuilds a company that already exists, e.g. when loading it from storage.</summary>
    public static Company Restore(Guid id, CompanyName name, WebsiteUrl website, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Company id must not be empty.", nameof(id));
        }

        return new Company(id, name, website, createdAt);
    }
}

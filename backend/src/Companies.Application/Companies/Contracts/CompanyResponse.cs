namespace Companies.Application.Companies.Contracts;

/// <summary>A stored company as exposed by the API.</summary>
/// <param name="Website">Canonical website URL, e.g. <c>https://www.verus.com</c>.</param>
/// <param name="Domain">Registrable domain, e.g. <c>verus.com</c>.</param>
public sealed record CompanyResponse(Guid Id, string Name, string Website, string Domain, DateTimeOffset CreatedAt)
{
    /// <summary>Search relevance in [0, 1]; only set when the request included a search term.</summary>
    public double? RelevanceScore { get; init; }
}

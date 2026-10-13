namespace Companies.Application.Companies.Contracts;

/// <summary>Filtering, search, sorting and paging for the company collection. Every field is optional.</summary>
public sealed record CompanyQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxFilterLength = 200;

    /// <summary>Partial or full company name, case-insensitive.</summary>
    public string? Name { get; init; }

    /// <summary>A domain (<c>acme.com</c>, <c>https://www.acme.com</c>) matches on the registrable domain; a fragment (<c>acme</c>) on the host.</summary>
    public string? Domain { get; init; }

    /// <summary>Free-text relevance search (<c>q</c>). Results are ordered by relevance unless <see cref="Sort"/> says otherwise.</summary>
    public string? Search { get; init; }

    /// <summary><c>name</c>, <c>createdAt</c> or <c>relevance</c>; prefix with <c>-</c> for descending.</summary>
    public string? Sort { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}

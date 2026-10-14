using System.Text.Json.Serialization;
using Companies.Application.Companies.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Companies.Api.Controllers;

/// <summary>Query-string binding for <c>GET /api/companies</c>; maps 1:1 onto <see cref="CompanyQuery"/>.</summary>
public sealed record CompanyListParameters
{
    /// <summary>Partial or full company name (case-insensitive).</summary>
    [FromQuery(Name = "name")]
    public string? Name { get; init; }

    /// <summary>Website domain: <c>acme.com</c> / a URL matches the registrable domain, <c>acme</c> any host containing it.</summary>
    [FromQuery(Name = "domain")]
    public string? Domain { get; init; }

    /// <summary>Free-text relevance search; results are ordered by relevance and carry a <c>relevanceScore</c>.</summary>
    /// <remarks>
    /// <see cref="JsonPropertyNameAttribute"/> keeps the validation metadata name equal to the bound key
    /// ("q"); otherwise the JSON-name metadata provider names it "search" and the entry stays unvalidated.
    /// </remarks>
    [FromQuery(Name = "q")]
    [JsonPropertyName("q")]
    public string? Search { get; init; }

    /// <summary><c>name</c>, <c>-name</c>, <c>createdAt</c>, <c>-createdAt</c> or <c>relevance</c> (requires <c>q</c>).</summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; init; }

    /// <summary>1-based page number (default 1).</summary>
    [FromQuery(Name = "page")]
    public int? Page { get; init; }

    /// <summary>Items per page, 1–100 (default 20).</summary>
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    public CompanyQuery ToQuery() => new()
    {
        Name = Name,
        Domain = Domain,
        Search = Search,
        Sort = Sort,
        Page = Page ?? 1,
        PageSize = PageSize ?? CompanyQuery.DefaultPageSize,
    };
}

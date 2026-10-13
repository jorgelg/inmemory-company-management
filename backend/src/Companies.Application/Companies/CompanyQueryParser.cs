using Companies.Application.Companies.Contracts;
using Companies.Domain.Common;
using Companies.Domain.Companies;

namespace Companies.Application.Companies;

internal enum CompanySortField
{
    Name,
    CreatedAt,
    Relevance,
}

internal readonly record struct CompanySort(CompanySortField Field, bool Descending);

/// <summary>A <see cref="CompanyQuery"/> that passed validation, split into store filter and in-memory steps.</summary>
internal sealed record ParsedCompanyQuery(CompanyFilter Filter, string? Search, CompanySort Sort, int Page, int PageSize);

/// <summary>Validates a <see cref="CompanyQuery"/> and translates it into filter, search, sort and paging.</summary>
internal static class CompanyQueryParser
{
    public static Result<ParsedCompanyQuery> Parse(CompanyQuery query)
    {
        if (query.Page < 1)
        {
            return CompanyApplicationErrors.InvalidPage;
        }

        if (query.PageSize is < 1 or > CompanyQuery.MaxPageSize)
        {
            return CompanyApplicationErrors.InvalidPageSize;
        }

        if ((TooLong(query.Name, "name") ?? TooLong(query.Domain, "domain") ?? TooLong(query.Search, "q")) is { } tooLong)
        {
            return tooLong;
        }

        var search = Clean(query.Search);
        var sort = ParseSort(query.Sort, hasSearch: search is not null);
        if (sort.IsFailure)
        {
            return sort.Error;
        }

        var filter = BuildFilter(Clean(query.Name), Clean(query.Domain));
        return new ParsedCompanyQuery(filter, search, sort.Value, query.Page, query.PageSize);
    }

    private static Result<CompanySort> ParseSort(string? sort, bool hasSearch)
    {
        var value = Clean(sort);
        if (value is null)
        {
            return new CompanySort(hasSearch ? CompanySortField.Relevance : CompanySortField.Name, Descending: false);
        }

        var descending = value.StartsWith('-');
        var key = descending ? value[1..] : value;

        if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            return new CompanySort(CompanySortField.Name, descending);
        }

        if (key.Equals("createdAt", StringComparison.OrdinalIgnoreCase))
        {
            return new CompanySort(CompanySortField.CreatedAt, descending);
        }

        if (key.Equals("relevance", StringComparison.OrdinalIgnoreCase) && !descending)
        {
            return hasSearch
                ? new CompanySort(CompanySortField.Relevance, Descending: false)
                : CompanyApplicationErrors.RelevanceSortRequiresSearch;
        }

        return CompanyApplicationErrors.InvalidSort;
    }

    private static CompanyFilter BuildFilter(string? name, string? domain)
    {
        if (domain is null)
        {
            return new CompanyFilter(NameContains: name);
        }

        // "acme.com", "www.acme.com" or a full URL identify one registrable domain; anything else is a fragment.
        var candidate = domain.Contains("://", StringComparison.Ordinal) ? domain : $"https://{domain}";
        var website = WebsiteUrl.Create(candidate);

        return website.IsSuccess
            ? new CompanyFilter(NameContains: name, RegistrableDomain: website.Value.RegistrableDomain)
            : new CompanyFilter(NameContains: name, DomainContains: domain.ToLowerInvariant());
    }

    private static Error? TooLong(string? value, string parameter) =>
        value?.Trim().Length > CompanyQuery.MaxFilterLength ? CompanyApplicationErrors.FilterTooLong(parameter) : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

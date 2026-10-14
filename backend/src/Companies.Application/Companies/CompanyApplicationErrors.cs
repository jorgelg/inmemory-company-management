using System.Globalization;
using Companies.Application.Companies.Contracts;
using Companies.Application.Relevance;
using Companies.Domain.Common;
using Companies.Domain.Companies;

namespace Companies.Application.Companies;

/// <summary>Errors raised by the company use cases. Targets are the API's field and query-parameter names.</summary>
public static class CompanyApplicationErrors
{
    public static readonly Error InvalidPage = Error.Validation(
        "query.invalid_page", "Page must be 1 or greater.", "page");

    public static readonly Error InvalidPageSize = Error.Validation(
        "query.invalid_page_size", $"Page size must be between 1 and {CompanyQuery.MaxPageSize}.", "pageSize");

    public static readonly Error InvalidSort = Error.Validation(
        "query.invalid_sort", "Sort must be one of: name, -name, createdAt, -createdAt, relevance.", "sort");

    public static readonly Error RelevanceSortRequiresSearch = Error.Validation(
        "query.relevance_sort_requires_search", "Sorting by relevance requires a search term (q).", "sort");

    public static Error NotFound(Guid id) =>
        Error.NotFound("company.not_found", $"Company '{id}' was not found.");

    public static Error DuplicateWebsite(string registrableDomain) => Error.Conflict(
        "company.duplicate_website", $"A company with website domain '{registrableDomain}' already exists.", CompanyErrors.WebsiteField);

    public static Error NotRelevant(CompanyName name, WebsiteUrl website, RelevanceVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(verdict);

        // Every strategy that ran is cited, so "the website could not be read" is visible, not just the best score.
        var reasons = string.Join("; ", verdict.Evidence.Select(evidence => evidence.Reason));

        return Error.Validation("company.not_relevant", string.Create(CultureInfo.InvariantCulture,
            $"Company name '{name}' does not appear to be related to website '{website.RegistrableDomain}': " +
            $"{reasons} (best score {verdict.Best.Score:0.00}, threshold {verdict.Threshold:0.00})."));
    }

    public static Error FilterTooLong(string parameter) => Error.Validation(
        "query.filter_too_long", $"'{parameter}' must be at most {CompanyQuery.MaxFilterLength} characters.", parameter);
}

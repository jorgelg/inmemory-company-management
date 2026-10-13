using Companies.Application.Companies.Contracts;
using Companies.Application.Relevance;
using Companies.Application.Search;
using Companies.Domain.Common;
using Companies.Domain.Companies;
using Microsoft.Extensions.Logging;

namespace Companies.Application.Companies;

internal sealed class CompanyService(
    ICompanyRepository repository,
    IRelevanceValidator relevanceValidator,
    ICompanySearchRanker searchRanker,
    TimeProvider timeProvider,
    ILogger<CompanyService> logger) : ICompanyService
{
    public async Task<Result<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = CompanyName.Create(request.Name);
        if (name.IsFailure)
        {
            return Rejected(name.Error);
        }

        var website = WebsiteUrl.Create(request.Website);
        if (website.IsFailure)
        {
            return Rejected(website.Error);
        }

        var domain = website.Value.RegistrableDomain;

        // Cheap uniqueness check first: it avoids a relevance check (possibly an HTTP fetch) for a known duplicate.
        if (await repository.ExistsByDomainAsync(domain, cancellationToken).ConfigureAwait(false))
        {
            return Rejected(CompanyApplicationErrors.DuplicateWebsite(domain));
        }

        var verdict = await relevanceValidator.EvaluateAsync(name.Value, website.Value, cancellationToken).ConfigureAwait(false);
        if (!verdict.IsRelevant)
        {
            return Rejected(CompanyApplicationErrors.NotRelevant(name.Value, website.Value, verdict));
        }

        var company = Company.Create(name.Value, website.Value, timeProvider);

        // The authoritative uniqueness check: atomic in the store, covers a concurrent registration.
        if (!await repository.TryAddAsync(company, cancellationToken).ConfigureAwait(false))
        {
            return Rejected(CompanyApplicationErrors.DuplicateWebsite(domain));
        }

        logger.CompanyCreated(company.Id, company.Name.Value, domain);
        return company.ToResponse();
    }

    public async Task<Result<CompanyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (company is null)
        {
            logger.CompanyNotFound(id);
            return CompanyApplicationErrors.NotFound(id);
        }

        return company.ToResponse();
    }

    public async Task<Result<PagedResponse<CompanyResponse>>> SearchAsync(CompanyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parsed = CompanyQueryParser.Parse(query);
        if (parsed.IsFailure)
        {
            return Rejected(parsed.Error);
        }

        var criteria = parsed.Value;
        var candidates = await repository.ListAsync(criteria.Filter, cancellationToken).ConfigureAwait(false);

        IEnumerable<ScoredCompany> matches = criteria.Search is null
            ? candidates.Select(company => new ScoredCompany(company, Score: 0))
            : searchRanker.Rank(candidates, criteria.Search);

        var ordered = Sort(matches, criteria.Sort).ToList();
        var items = ordered
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(match => match.Company.ToResponse(criteria.Search is null ? null : match.Score))
            .ToList();

        logger.CompaniesSearched(criteria.Filter, criteria.Search, ordered.Count);
        return new PagedResponse<CompanyResponse>(items, criteria.Page, criteria.PageSize, ordered.Count);
    }

    private static IEnumerable<ScoredCompany> Sort(IEnumerable<ScoredCompany> matches, CompanySort sort) => sort switch
    {
        // The ranker already returns best-first, ties by name.
        { Field: CompanySortField.Relevance } => matches,
        { Field: CompanySortField.Name, Descending: false } => matches
            .OrderBy(m => m.Company.Name.Value, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Company.CreatedAt),
        { Field: CompanySortField.Name, Descending: true } => matches
            .OrderByDescending(m => m.Company.Name.Value, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Company.CreatedAt),
        { Field: CompanySortField.CreatedAt, Descending: false } => matches
            .OrderBy(m => m.Company.CreatedAt).ThenBy(m => m.Company.Id),
        { Field: CompanySortField.CreatedAt, Descending: true } => matches
            .OrderByDescending(m => m.Company.CreatedAt).ThenByDescending(m => m.Company.Id),
        _ => throw new InvalidOperationException($"Unsupported sort {sort}."),
    };

    private Error Rejected(Error error)
    {
        logger.RequestRejected(error.Type, error.Code, error.Message);
        return error;
    }
}

internal static partial class CompanyServiceLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Company {CompanyId} '{CompanyName}' created for domain {Domain}")]
    public static partial void CompanyCreated(this ILogger logger, Guid companyId, string companyName, string domain);

    [LoggerMessage(Level = LogLevel.Information, Message = "Company {CompanyId} was not found")]
    public static partial void CompanyNotFound(this ILogger logger, Guid companyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected ({ErrorType} {ErrorCode}): {ErrorMessage}")]
    public static partial void RequestRejected(this ILogger logger, ErrorType errorType, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Company search {Filter} q='{Search}' matched {MatchCount}")]
    public static partial void CompaniesSearched(this ILogger logger, CompanyFilter filter, string? search, int matchCount);
}

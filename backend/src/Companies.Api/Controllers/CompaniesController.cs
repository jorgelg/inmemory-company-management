using Companies.Api.Http;
using Companies.Application.Companies;
using Companies.Application.Companies.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Companies.Api.Controllers;

/// <summary>
/// Company resource. Each action binds input, calls <see cref="ICompanyService"/> and maps the
/// result to HTTP; no business logic lives here. Action names have no <c>Async</c> suffix because
/// MVC trims it, which would break <see cref="ControllerBase.CreatedAtAction(string, object)"/>.
/// </summary>
[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(ICompanyService companies) : ControllerBase
{
    /// <summary>Registers a company after validating its name, website and their relevance.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ErrorResults.ProblemContentType)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict, ErrorResults.ProblemContentType)]
    public async Task<IActionResult> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await companies.CreateAsync(request, cancellationToken);

        return result.Match(
            company => CreatedAtAction(nameof(GetById), new { id = company.Id }, company),
            this.ToProblem);
    }

    /// <summary>Lists companies with optional filtering, relevance search, sorting and paging.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<CompanyResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ErrorResults.ProblemContentType)]
    public async Task<IActionResult> List([FromQuery] CompanyListParameters parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var result = await companies.SearchAsync(parameters.ToQuery(), cancellationToken);

        return result.Match<IActionResult>(Ok, this.ToProblem);
    }

    /// <summary>Gets one company by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ErrorResults.ProblemContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await companies.GetByIdAsync(id, cancellationToken);

        return result.Match<IActionResult>(Ok, this.ToProblem);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Companies.Api.Http;

/// <summary>
/// Replaces the default automatic 400 for model-binding and data-annotation failures: adds a
/// concise <c>detail</c> and a <c>code</c>, and hides serializer internals behind one "body" error.
/// </summary>
internal static class InvalidRequestResponseFactory
{
    public const string Code = "request.invalid";

    private const string BodyKey = "body";
    private const string BodyMessage = """Request body must be a JSON object, e.g. {"name": "Acme", "website": "https://www.acme.com"}.""";

    public static IActionResult Create(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errors = Normalize(context.ModelState);
        var detail = string.Join(" ", errors.SelectMany(e => e.Value).Distinct(StringComparer.Ordinal));

        var fields = new ModelStateDictionary();
        foreach (var (key, messages) in errors)
        {
            foreach (var message in messages)
            {
                fields.AddModelError(key, message);
            }
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext, fields, StatusCodes.Status400BadRequest, detail: detail.Length > 0 ? detail : "The request is invalid.");
        problem.Extensions["code"] = Code;

        return new BadRequestObjectResult(problem) { ContentTypes = { ErrorResults.ProblemContentType } };
    }

    private static Dictionary<string, string[]> Normalize(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            // "" / "request" / "$..." are body-level failures (missing body, invalid JSON, wrong shape).
            if (key.Length == 0 || key == "request" || key.StartsWith('$'))
            {
                errors[BodyKey] = [BodyMessage];
                continue;
            }

            errors[key] = [.. entry.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"'{key}' is invalid." : e.ErrorMessage)];
        }

        return errors;
    }
}

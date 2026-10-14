using Companies.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Companies.Api.Http;

/// <summary>
/// Maps an application <see cref="Error"/> to an RFC 9457 problem response. This is the single
/// place where error types become status codes.
/// </summary>
internal static class ErrorResults
{
    public const string ProblemContentType = "application/problem+json";

    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "The resource was not found."),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "The resource already exists."),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type."),
        };

        var factory = controller.ProblemDetailsFactory;
        ProblemDetails problem;

        // A field-level error is also listed under "errors", so a form can show it next to the field.
        if (error.Target is not null)
        {
            var fields = new ModelStateDictionary();
            fields.AddModelError(error.Target, error.Message);
            problem = factory.CreateValidationProblemDetails(controller.HttpContext, fields, status, title, detail: error.Message);
        }
        else
        {
            problem = factory.CreateProblemDetails(controller.HttpContext, status, title, detail: error.Message);
        }

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { ProblemContentType } };
    }
}

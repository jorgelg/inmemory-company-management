using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Companies.Api.IntegrationTests.Support;

[ShouldlyMethods]
internal static class HttpAssertions
{
    public static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    /// <summary>Asserts the status code and deserializes the JSON body.</summary>
    public static async Task<T> ShouldHaveJsonBody<T>(this HttpResponseMessage response, HttpStatusCode status)
        where T : class
    {
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json)).ShouldNotBeNull();
    }

    /// <summary>
    /// Asserts an RFC 9457 problem response: status, <c>application/problem+json</c>, a non-empty
    /// <c>detail</c>, a <c>traceId</c>, and (when given) the error <c>code</c> extension.
    /// </summary>
    public static async Task<JsonElement> ShouldBeProblem(this HttpResponseMessage response, HttpStatusCode status, string? code = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(status, body);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var problem = JsonDocument.Parse(body).RootElement.Clone();
        problem.ShouldSatisfyAllConditions(
            p => p.GetProperty("status").GetInt32().ShouldBe((int)status),
            p => p.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace(),
            p => p.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace(),
            p => p.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace());

        if (code is not null)
        {
            problem.GetProperty("code").GetString().ShouldBe(code);
        }

        return problem;
    }

    /// <summary>Returns the messages listed for <paramref name="field"/> in a validation problem's <c>errors</c>.</summary>
    public static string[] FieldErrors(this JsonElement problem, string field) =>
        problem.TryGetProperty("errors", out var errors) && errors.TryGetProperty(field, out var messages)
            ? [.. messages.EnumerateArray().Select(m => m.GetString()!)]
            : [];
}

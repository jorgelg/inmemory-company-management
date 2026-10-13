using Companies.Domain.Common;

namespace Companies.UnitTests.Support;

/// <summary>Shouldly-style assertions for <see cref="Result{T}"/>.</summary>
[ShouldlyMethods]
internal static class ResultShouldlyExtensions
{
    /// <summary>Asserts success and returns the value; on failure the error code and message are reported.</summary>
    public static T ShouldBeSuccess<T>(this Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new ShouldAssertException(
                $"Expected a successful result but got error '{result.Error.Code}': {result.Error.Message}");
        }

        return result.Value;
    }

    /// <summary>Asserts failure and returns the error.</summary>
    public static Error ShouldBeFailure<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new ShouldAssertException($"Expected a failed result but got value '{result.Value}'.");
        }

        return result.Error;
    }

    /// <summary>Asserts a validation failure with the given code and target field and a non-empty message.</summary>
    public static Error ShouldBeValidationError<T>(this Result<T> result, string code, string target)
    {
        var error = result.ShouldBeFailure();

        error.ShouldSatisfyAllConditions(
            () => error.Type.ShouldBe(ErrorType.Validation),
            () => error.Code.ShouldBe(code),
            () => error.Target.ShouldBe(target),
            () => error.Message.ShouldNotBeNullOrWhiteSpace());

        return error;
    }
}

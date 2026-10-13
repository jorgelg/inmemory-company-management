using System.Diagnostics.CodeAnalysis;

namespace Companies.Domain.Common;

/// <summary>
/// An expected failure: a stable machine-readable <see cref="Code"/>, a concise human-readable
/// <see cref="Message"/>, and optionally the input field (<see cref="Target"/>) it refers to.
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "C#-only codebase; 'Error' is the conventional name for a Result failure.")]
public sealed record Error(string Code, string Message, ErrorType Type, string? Target = null)
{
    public static Error Validation(string code, string message, string? target = null) =>
        new(code, message, ErrorType.Validation, target);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message, string? target = null) =>
        new(code, message, ErrorType.Conflict, target);
}

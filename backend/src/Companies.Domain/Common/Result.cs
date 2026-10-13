namespace Companies.Domain.Common;

/// <summary>
/// Either a value or an <see cref="Common.Error"/>. Expected failures (validation, not found,
/// conflict) travel as values instead of exceptions.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error) => _error = error;

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({_error!.Code}).");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot read the error of a successful result.");

    public static implicit operator Result<T>(T value) => FromValue(value);

    public static implicit operator Result<T>(Error error) => FromError(error);

#pragma warning disable CA1000 // Named alternates for the implicit operators (CA2225)
    public static Result<T> FromValue(T value) => new(value);

    public static Result<T> FromError(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
#pragma warning restore CA1000

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }
}

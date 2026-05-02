namespace CodeLogic.Core.Results;

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/> on success.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    /// <summary>Gets a value indicating whether the result represents a success.</summary>
    public bool IsSuccess { get; }
    /// <summary>Gets a value indicating whether the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;
    /// <summary>Gets the success value, or default if the result is a failure.</summary>
    public T? Value { get; }
    /// <summary>Gets the error, or null if the result is a success.</summary>
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Creates a successful result with the specified value.</summary>
    public static Result<T> Success(T value) => new(true, value, null);
    /// <summary>Creates a failed result with the specified error.</summary>
    public static Result<T> Failure(Error error) => new(false, default, error);

    /// <summary>Implicitly converts a value to a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
    /// <summary>Implicitly converts an error to a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Transforms the success value using the specified mapper function.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!);

    /// <summary>Converts this generic result to a non-generic <see cref="Result"/>.</summary>
    public Result ToResult() =>
        IsSuccess ? Result.Success() : Result.Failure(Error!);

    /// <summary>Executes the specified action if the result is a success.</summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>Executes the specified action if the result is a failure.</summary>
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }

    /// <summary>Returns the success value, or the specified default if the result is a failure.</summary>
    public T ValueOrDefault(T defaultValue) => IsSuccess ? Value! : defaultValue;
    /// <summary>Returns the success value, or throws if the result is a failure.</summary>
    public T ValueOrThrow() =>
        IsSuccess ? Value! : throw new InvalidOperationException($"Result is failure: {Error}");

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}

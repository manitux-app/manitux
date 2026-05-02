namespace CodeLogic.Core.Results;

/// <summary>Represents the outcome of an operation that can succeed or fail.</summary>
public readonly struct Result
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error details when the operation failed; null on success.</summary>
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result with the specified error.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Implicitly converts an <see cref="Error"/> to a failed result.</summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>Executes the action only when the result is a success.</summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    /// <summary>Executes the action only when the result is a failure.</summary>
    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}

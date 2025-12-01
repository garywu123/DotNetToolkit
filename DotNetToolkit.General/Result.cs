namespace DotNetToolkit.General;

/// <summary>
/// Simple Result wrapper representing success or failure with an optional value.
/// </summary>
/// <typeparam name="T">Value type on success.</typeparam>
public sealed class Result<T>
{
    private Result(T? value, string? error, bool isSuccess)
    {
        Value = value!;
        Error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// True when the result represents success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True when the result represents failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Value for successful results. Accessing when <see cref="IsSuccess"/> is false may return default.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Error message for failed results.
    /// </summary>
    public string? Error { get; }

    public static Result<T> Ok(T value)
    {
        // Allow null for T when caller explicitly passes null for nullable types.
        return new Result<T>(value, null, true);
    }

    public static Result<T> Fail(string error)
    {
        return string.IsNullOrWhiteSpace(error)
            ? throw new ArgumentException("error must be provided.", nameof(error))
            : new Result<T>(default, error, false);
    }
}

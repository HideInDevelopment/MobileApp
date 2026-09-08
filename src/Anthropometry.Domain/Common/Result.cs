namespace Anthropometry.Domain.Common;

public sealed class Result
{
    private Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public DomainError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(DomainError error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Failure<T>(DomainError error) => Result<T>.CreateFailure(error);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T Value { get; }

    public DomainError? Error { get; }

    internal static Result<T> CreateSuccess(T value) => new(true, value, null);

    internal static Result<T> CreateFailure(DomainError error) => new(false, default!, error);
}

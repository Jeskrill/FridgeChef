using System.Diagnostics.CodeAnalysis;

namespace FridgeChef.SharedKernel;

public sealed record DomainError(string Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}

public sealed class Result<TValue>
{
    private readonly TValue? _value;
    private readonly DomainError? _error;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(DomainError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public DomainError Error => !IsSuccess
        ? _error
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static implicit operator Result<TValue>(TValue value) => new(value);
    public static implicit operator Result<TValue>(DomainError error) => new(error);

    public TResult Match<TResult>(
        Func<TValue, TResult> success,
        Func<DomainError, TResult> failure) =>
        IsSuccess ? success(_value) : failure(_error);
}

public sealed class Result
{
    private readonly DomainError? _error;

    private Result() { IsSuccess = true; _error = null; }
    private Result(DomainError error) { IsSuccess = false; _error = error; }

    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public DomainError Error => !IsSuccess
        ? _error
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result Success() => new();
    public static Result Failure(DomainError error) => new(error);

    public static implicit operator Result(DomainError error) => Failure(error);
}

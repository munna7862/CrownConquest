using System.Diagnostics.CodeAnalysis;

namespace CrownConquest.Domain.Common;

/// <summary>
/// Allocation-free Result monad for deterministic operation results.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly GameError _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on failed Result: {_error}");

    public GameError Error => IsFailure ? _error : GameError.None;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = GameError.None;
    }

    private Result(GameError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(GameError error) => new(error);

    public bool TryGetValue([NotNullWhen(true)] out T? value, out GameError error)
    {
        if (IsSuccess)
        {
            value = _value!;
            error = GameError.None;
            return true;
        }

        value = default;
        error = _error;
        return false;
    }
}

/// <summary>
/// Non-generic Result for void-returning domain operations.
/// </summary>
public readonly struct Result
{
    private readonly GameError _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public GameError Error => IsFailure ? _error : GameError.None;

    private Result(bool isSuccess, GameError error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result Success() => new(true, GameError.None);
    public static Result Failure(GameError error) => new(false, error);
}

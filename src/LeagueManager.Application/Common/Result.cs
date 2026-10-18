namespace LeagueManager.Application.Common;

// Expected failures (invalid input, missing team, duplicate name) are returned as values instead of thrown,
// so the method signature shows that a handler can fail and the caller is forced to handle both outcomes.
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value) => _value = value;
    private Result(Error error) => _error = error;

    public bool IsSuccess => _error is null;

    // Implicit conversions let handlers simply `return dto;` or `return Error.NotFound(...);`.
    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        _error is null ? onSuccess(_value!) : onFailure(_error);
}

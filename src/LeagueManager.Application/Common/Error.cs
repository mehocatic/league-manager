namespace LeagueManager.Application.Common;

// The type says what went wrong in business terms; only the Api layer translates it into an HTTP status code.
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    ExternalService
}

public sealed record Error(ErrorType Type, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);
    public static Error ExternalService(string message) => new(ErrorType.ExternalService, message);
}

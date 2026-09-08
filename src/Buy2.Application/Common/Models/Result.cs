namespace Buy2.Application.Common.Models;

public enum ResultErrorType
{
    None,
    Validation,
    NotFound,
    Conflict
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public ResultErrorType ErrorType { get; }

    public bool IsNotFound => !IsSuccess && ErrorType == ResultErrorType.NotFound;
    public bool IsConflict => !IsSuccess && ErrorType == ResultErrorType.Conflict;
    public bool IsValidationError => !IsSuccess && ErrorType == ResultErrorType.Validation;

    private Result(bool isSuccess, T? value, string? errorMessage, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, ResultErrorType.None);

    public static Result<T> ValidationFailure(string message) =>
        new(false, default, message, ResultErrorType.Validation);

    public static Result<T> NotFound(string message) =>
        new(false, default, message, ResultErrorType.NotFound);

    public static Result<T> Conflict(string message) =>
        new(false, default, message, ResultErrorType.Conflict);
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public ResultErrorType ErrorType { get; }

    public bool IsNotFound => !IsSuccess && ErrorType == ResultErrorType.NotFound;
    public bool IsConflict => !IsSuccess && ErrorType == ResultErrorType.Conflict;
    public bool IsValidationError => !IsSuccess && ErrorType == ResultErrorType.Validation;

    private Result(bool isSuccess, string? errorMessage, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
    }

    public static Result Success() =>
        new(true, null, ResultErrorType.None);

    public static Result ValidationFailure(string message) =>
        new(false, message, ResultErrorType.Validation);

    public static Result NotFound(string message) =>
        new(false, message, ResultErrorType.NotFound);

    public static Result Conflict(string message) =>
        new(false, message, ResultErrorType.Conflict);
}

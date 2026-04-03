namespace Entegrasyon.Entity.Results;

/// <summary>
/// Modern record-based result type. Use static factory methods Ok/Fail.
/// Replaces SuccessDataResult/ErrorDataResult class hierarchy.
/// </summary>
public readonly record struct Result<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
    public static Result<T> Ok(T data, string message) => new() { Success = true, Data = data, Message = message };
    public static Result<T> Fail(string message) => new() { Success = false, Message = message };
    public static Result<T> Fail(string message, IReadOnlyList<string> errors) => new() { Success = false, Message = message, Errors = errors };

    /// <summary>Implicit conversion from old SuccessDataResult for gradual migration.</summary>
    public static implicit operator Result<T>(SuccessDataResult<T> old) => new() { Success = true, Data = old.Data, Message = old.Message };

    /// <summary>Implicit conversion from old ErrorDataResult for gradual migration.</summary>
    public static implicit operator Result<T>(ErrorDataResult<T> old) => new() { Success = false, Data = old.Data, Message = old.Message };
}

/// <summary>
/// Modern record-based result type (non-generic). Use static factory methods Ok/Fail.
/// Replaces SuccessResult/ErrorResult class hierarchy.
/// </summary>
public readonly record struct ResultRecord
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static ResultRecord Ok() => new() { Success = true };
    public static ResultRecord Ok(string message) => new() { Success = true, Message = message };
    public static ResultRecord Fail(string message) => new() { Success = false, Message = message };

    /// <summary>Implicit conversion from old SuccessResult for gradual migration.</summary>
    public static implicit operator ResultRecord(SuccessResult old) => new() { Success = true, Message = old.Message };

    /// <summary>Implicit conversion from old ErrorResult for gradual migration.</summary>
    public static implicit operator ResultRecord(ErrorResult old) => new() { Success = false, Message = old.Message };
}

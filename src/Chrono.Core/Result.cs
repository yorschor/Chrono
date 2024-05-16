#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Chrono.Core;

public abstract class Result
{
    public bool Success { get; protected init; }
    public bool Failure => !Success;
}

public abstract class Result<T> : Result
{
    private readonly T _data;

    protected Result(T data)
    {
        Data = data;
    }

    public T Data
    {
        get => Success
            ? _data
            : throw new Exception($"You can't access .{nameof(Data)} when .{nameof(Success)} is false");
        init => _data = value;
    }
}

public class SuccessResult : Result
{
    public SuccessResult()
    {
        Success = true;
    }
}

public class SuccessResult<T> : Result<T>
{
    public SuccessResult(T data) : base(data)
    {
        Success = true;
    }
}

public class ErrorResult : Result, IErrorResult
{
    public ErrorResult(string message) : this(message, Array.Empty<Error>())
    {
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors)
    {
        Message = message;
        Success = false;
        Errors = errors ?? Array.Empty<Error>();
    }

    public string Message { get; }
    public IReadOnlyCollection<Error> Errors { get; }
}

public class ErrorResult<T> : Result<T>, IErrorResult
{
    public ErrorResult(string message) : this(message, Array.Empty<Error>())
    {
    }

    public ErrorResult(string message, IReadOnlyCollection<Error> errors) : base(default!)
    {
        Message = message;
        Success = false;
        Errors = errors ?? Array.Empty<Error>();
    }

    public string Message { get; set; }
    public IReadOnlyCollection<Error> Errors { get; }
}

public class Error(string code, string details)
{
    public Error(string details) : this(null!, details)
    {
    }

    public string Code { get; } = code;
    public string Details { get; } = details;
}

public interface IErrorResult
{
    string Message { get; }
    IReadOnlyCollection<Error> Errors { get; }
}
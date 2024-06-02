#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Chrono.Core.Helpers
{
    public abstract class Result
    {
        public bool Success { get; protected set; }
        public bool Failure => !Success;
        
        public static OkResult Ok() => new();
        public static OkResult<T> Ok<T>(T data) => new(data);
        public static ErrorResult Error(string message) => new(message);
        public static ErrorResult Error(string message, IReadOnlyCollection<Error> errors) => new(message, errors);
        public static ErrorResult<T> Error<T>(string message) => new(message);
        public static ErrorResult<T> Error<T>(string message, IReadOnlyCollection<Error> errors) => new(message, errors);
        public static ErrorResult<T> Error<T>(IErrorResult errorResult) => new(errorResult.Message, errorResult.Errors);
    }

    public abstract class Result<T> : Result
    {
        private T _data;

        protected Result(T data)
        {
            _data = data;
        }

        public T Data
        {
            get => Success
                ? _data
                : throw new Exception($"You can't access .{nameof(Data)} when .{nameof(Success)} is false");
            protected set => _data = value;
        }
    }

    public class OkResult : Result
    {
        public OkResult()
        {
            Success = true;
        }
    }

    public class OkResult<T> : Result<T>
    {
        public OkResult(T data) : base(data)
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

        public ErrorResult(string message, IReadOnlyCollection<Error> errors) : base(default(T))
        {
            Message = message;
            Success = false;
            Errors = errors ?? Array.Empty<Error>();
        }

        public string Message { get; set; }
        public IReadOnlyCollection<Error> Errors { get; }
    }

    public class Error
    {
        public Error(string code, string details)
        {
            Code = code;
            Details = details;
        }

        public Error(string details) : this(null, details)
        {
        }

        public string Code { get; }
        public string Details { get; }
    }

    public interface IErrorResult
    {
        string Message { get; }
        IReadOnlyCollection<Error> Errors { get; }
    }
}

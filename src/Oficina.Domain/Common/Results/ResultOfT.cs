namespace Oficina.Domain.Common.Results;

public sealed class Result<T> : Result, IResultFactory<Result<T>>
{
    private readonly T? _value;

    private Result(T? value, Error? error)
        : base(error) => _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException(nameof(Value));

    public static Result<T> Success(T value) => new(value, null);

    public static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public static implicit operator Result<T>(T value) => Success(value);
}

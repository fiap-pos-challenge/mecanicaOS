using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.UnitTests.Domain.Common.Results;

public sealed class ResultTests
{
    private static readonly Error SampleError = new("sample.code", "Mensagem", ErrorType.Conflict);

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_KeepsError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        var result = Result<int>.Failure(SampleError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Validation_CarriesFieldErrors()
    {
        var fields = new[] { new FieldError("name", "customer.nameRequired", "Informe o nome.") };

        var error = Error.Validation(fields);

        Assert.Equal(CommonErrors.ValidationFailed.Code, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Single(error.Fields);
    }

    [Fact]
    public void FactoryInterface_CreatesTypedFailure()
    {
        var result = CreateFailure<Result<string>>(SampleError);

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void FactoryInterface_CreatesUntypedFailure()
    {
        var result = CreateFailure<Result>(SampleError);

        Assert.True(result.IsFailure);
    }

    private static TResult CreateFailure<TResult>(Error error)
        where TResult : Result, IResultFactory<TResult> => TResult.Failure(error);
}

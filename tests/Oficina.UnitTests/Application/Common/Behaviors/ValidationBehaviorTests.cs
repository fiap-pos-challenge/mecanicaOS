using FluentValidation;
using MediatR;
using Oficina.Application.Common.Behaviors;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.UnitTests.Application.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<int>>(Array.Empty<IValidator<SampleRequest>>());

        var result = await behavior.Handle(new SampleRequest("Ana", 1), _ => Task.FromResult(Result.Success(7)), CancellationToken.None);

        Assert.Equal(7, result.Value);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<int>>(new[] { new SampleRequestValidator() });

        var result = await behavior.Handle(new SampleRequest("Ana", 1), _ => Task.FromResult(Result.Success(7)), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShortCircuitsWithCamelCaseFieldErrors()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result<int>>(new[] { new SampleRequestValidator() });
        var nextCalled = false;

        var result = await behavior.Handle(
            new SampleRequest(string.Empty, 0),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(7));
            },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.Equal(CommonErrors.ValidationFailed.Code, result.Error!.Code);
        Assert.Collection(
            result.Error.Fields.OrderBy(field => field.Field, StringComparer.Ordinal),
            field => Assert.Equal(new FieldError("page", CommonErrors.InvalidPage.Code, CommonErrors.InvalidPage.Message), field),
            field => Assert.Equal(new FieldError("personName", CommonErrors.RequiredField.Code, CommonErrors.RequiredField.Message), field));
    }

    [Fact]
    public async Task Handle_NestedProperty_UsesCamelCaseOnEverySegment()
    {
        var behavior = new ValidationBehavior<NestedRequest, Result>(new[] { new NestedRequestValidator() });

        var result = await behavior.Handle(new NestedRequest(new SampleRequest("Ana", 0)), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.Equal("inner.page", Assert.Single(result.Error!.Fields).Field);
    }

    [Fact]
    public async Task Handle_FactoryRuleFailure_UsesTheDomainErrorCode()
    {
        var behavior = new ValidationBehavior<SampleRequest, Result>(new[] { new FactoryValidator() });

        var result = await behavior.Handle(new SampleRequest("Ana", 1), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        var field = Assert.Single(result.Error!.Fields);
        Assert.Equal(CommonErrors.InvalidEmail.Code, field.Code);
        Assert.Equal("personName", field.Field);
    }

    public sealed record SampleRequest(string PersonName, int Page) : IRequest<Result<int>>;

    public sealed record NestedRequest(SampleRequest Inner) : IRequest<Result>;

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(request => request.PersonName).NotEmpty().WithError(CommonErrors.RequiredField);
            RuleFor(request => request.Page).ValidPage();
        }
    }

    private sealed class NestedRequestValidator : AbstractValidator<NestedRequest>
    {
        public NestedRequestValidator() => RuleFor(request => request.Inner).SetValidator(new SampleRequestValidator());
    }

    private sealed class FactoryValidator : AbstractValidator<SampleRequest>
    {
        public FactoryValidator() => RuleFor(request => request.PersonName).MustCreate(Oficina.Domain.Common.ValueObjects.Email.Create);
    }
}

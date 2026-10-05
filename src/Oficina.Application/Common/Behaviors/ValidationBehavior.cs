using System.Text.Json;
using FluentValidation;
using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result, IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var fields = results
            .SelectMany(result => result.Errors)
            .Select(failure => new FieldError(ToCamelCasePath(failure.PropertyName), failure.ErrorCode, failure.ErrorMessage))
            .ToArray();

        return fields.Length == 0 ? await next(cancellationToken) : TResponse.Failure(Error.Validation(fields));
    }

    private static string ToCamelCasePath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

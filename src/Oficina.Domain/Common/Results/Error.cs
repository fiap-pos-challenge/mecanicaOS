using Oficina.Domain.Common.Errors;

namespace Oficina.Domain.Common.Results;

public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyCollection<FieldError> Fields)
{
    public Error(string code, string message, ErrorType type)
        : this(code, message, type, Array.Empty<FieldError>())
    {
    }

    public static Error Validation(IReadOnlyCollection<FieldError> fields) =>
        CommonErrors.ValidationFailed with { Fields = fields };
}

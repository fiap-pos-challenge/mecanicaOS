namespace Oficina.Domain.Common.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
    Unauthorized,
    Forbidden,
    Unexpected,
}

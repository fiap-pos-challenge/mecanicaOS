using Oficina.Domain.Common.Results;

namespace Oficina.Application.Abstractions.Persistence;

public sealed class CommitFailedException(Error error) : Exception(error.Code)
{
    public Error Error => error;
}

using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.ReactivateService;

public sealed class ReactivateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork)
    : IRequestHandler<ReactivateServiceCommand, Result>
{
    public async Task<Result> Handle(ReactivateServiceCommand command, CancellationToken cancellationToken)
    {
        if (await services.GetByIdAsync(command.Id, cancellationToken) is not { } service)
        {
            return CatalogErrors.NotFound;
        }

        var reactivated = service.Reactivate();
        if (reactivated.IsFailure)
        {
            return reactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}

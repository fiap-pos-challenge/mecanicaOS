using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.DeactivateService;

public sealed class DeactivateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateServiceCommand, Result>
{
    public async Task<Result> Handle(DeactivateServiceCommand command, CancellationToken cancellationToken)
    {
        if (await services.GetByIdAsync(command.Id, cancellationToken) is not { } service)
        {
            return CatalogErrors.NotFound;
        }

        var deactivated = service.Deactivate();
        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}

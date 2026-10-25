using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.DeactivatePackage;

public sealed class DeactivatePackageCommandHandler(IPackageRepository packages, IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivatePackageCommand, Result>
{
    public async Task<Result> Handle(DeactivatePackageCommand command, CancellationToken cancellationToken)
    {
        if (await packages.GetByIdAsync(command.Id, cancellationToken) is not { } package)
        {
            return PackageErrors.NotFound;
        }

        var deactivated = package.Deactivate();
        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}

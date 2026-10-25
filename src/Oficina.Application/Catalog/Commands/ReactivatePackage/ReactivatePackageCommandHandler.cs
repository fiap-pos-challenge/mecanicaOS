using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.ReactivatePackage;

public sealed class ReactivatePackageCommandHandler(IPackageRepository packages, IUnitOfWork unitOfWork)
    : IRequestHandler<ReactivatePackageCommand, Result>
{
    public async Task<Result> Handle(ReactivatePackageCommand command, CancellationToken cancellationToken)
    {
        if (await packages.GetByIdAsync(command.Id, cancellationToken) is not { } package)
        {
            return PackageErrors.NotFound;
        }

        var reactivated = package.Reactivate();
        if (reactivated.IsFailure)
        {
            return reactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}

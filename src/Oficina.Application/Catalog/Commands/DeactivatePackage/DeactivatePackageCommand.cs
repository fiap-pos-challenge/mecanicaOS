using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.DeactivatePackage;

public sealed record DeactivatePackageCommand(Guid Id) : IRequest<Result>;

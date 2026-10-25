using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.ReactivatePackage;

public sealed record ReactivatePackageCommand(Guid Id) : IRequest<Result>;

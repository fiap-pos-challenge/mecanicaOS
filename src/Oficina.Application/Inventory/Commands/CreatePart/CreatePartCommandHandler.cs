using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Application.Inventory.Commands.CreatePart;

public sealed class CreatePartCommandHandler(IPartRepository parts, IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(CreatePartCommand command, CancellationToken cancellationToken)
    {
        var code = PartCode.Create(command.Code);
        var unitPrice = Money.Create(command.UnitPrice);
        if ((code.Error ?? unitPrice.Error) is { } invalidValue)
        {
            return invalidValue;
        }

        var part = Part.Create(code.Value, command.Name, command.Type, command.UnitOfMeasure, unitPrice.Value, command.MinimumStock);
        if (part.Error is { } invalidPart)
        {
            return invalidPart;
        }

        parts.Add(part.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return part.Value.ToDto();
    }
}

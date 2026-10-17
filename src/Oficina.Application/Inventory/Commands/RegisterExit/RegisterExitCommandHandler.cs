using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.RegisterExit;

public sealed class RegisterExitCommandHandler(IPartRepository parts, IInventoryQueries queries, ICurrentUser currentUser, IUnitOfWork unitOfWork) : IRequestHandler<RegisterExitCommand, Result<StockMovementDto>>
{
    public async Task<Result<StockMovementDto>> Handle(RegisterExitCommand request, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(request.PartId, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound with { Type = ErrorType.Validation, Fields = [new FieldError("partId", InventoryErrors.NotFound.Code, InventoryErrors.NotFound.Message)] };
        }

        var quantity = Quantity.Create(request.Quantity);
        if (quantity.IsFailure)
        {
            return InventoryErrors.FractionalPart;
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Saída manual" : request.Reason.Trim();
        if (part.RegisterExit(quantity.Value, reason, request.ServiceOrderNumber, currentUser.UserId).Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return (await queries.MovementAsync(part.StockMovements.Last().Id, cancellationToken))!;
    }
}

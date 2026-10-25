using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.UpdateSupplier;

public sealed class UpdateSupplierCommandHandler(ISupplierRepository suppliers, IUnitOfWork unitOfWork) : IRequestHandler<UpdateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (await suppliers.GetByIdAsync(request.Id, cancellationToken) is not { } supplier)
        {
            return InventoryErrors.SupplierNotFound;
        }

        if (supplier.Change(request.Name, request.Document, request.Email, request.Phone, request.LeadTimeDays).Error is { } invalid)
        {
            return invalid;
        }

        if (supplier.Document is { } document && await suppliers.DocumentExistsAsync(document, supplier.Id, cancellationToken))
        {
            return InventoryErrors.DuplicateSupplier;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new SupplierDto(supplier.Id, supplier.Name, supplier.Document, supplier.Email, supplier.Phone, supplier.LeadTimeDays, supplier.IsActive);
    }
}

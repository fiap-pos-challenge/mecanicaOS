using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.CreateSupplier;

public sealed class CreateSupplierCommandHandler(ISupplierRepository suppliers, IUnitOfWork unitOfWork) : IRequestHandler<CreateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var created = Supplier.Create(request.Name, request.Document, request.Email, request.Phone, request.LeadTimeDays);
        if (created.Error is { } invalid)
        {
            return invalid;
        }

        var supplier = created.Value;
        if (supplier.Document is { } document && await suppliers.DocumentExistsAsync(document, null, cancellationToken))
        {
            return InventoryErrors.DuplicateSupplier;
        }

        suppliers.Add(supplier);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new SupplierDto(supplier.Id, supplier.Name, supplier.Document, supplier.Email, supplier.Phone, supplier.LeadTimeDays, supplier.IsActive);
    }
}

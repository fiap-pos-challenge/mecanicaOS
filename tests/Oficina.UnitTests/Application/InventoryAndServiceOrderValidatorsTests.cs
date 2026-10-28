using FluentValidation;
using Oficina.Application.Inventory.Commands.CreatePart;
using Oficina.Application.Inventory.Commands.DeactivatePart;
using Oficina.Application.Inventory.Commands.ReactivatePart;
using Oficina.Application.Inventory.Commands.RegisterStockEntry;
using Oficina.Application.Inventory.Commands.UpdatePart;
using Oficina.Application.Inventory.Queries.GetPartById;
using Oficina.Application.Inventory.Queries.ListParts;
using Oficina.Application.ServiceOrders.Commands.AddPartItem;
using Oficina.Application.ServiceOrders.Commands.AddServiceItem;
using Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;
using Oficina.Application.ServiceOrders.Commands.DeliverServiceOrder;
using Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;
using Oficina.Application.ServiceOrders.Commands.RejectQuote;
using Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;
using Oficina.Application.ServiceOrders.Commands.RemoveItem;
using Oficina.Application.ServiceOrders.Commands.SendQuote;
using Oficina.Application.ServiceOrders.Commands.StartDiagnosis;
using Oficina.Domain.Inventory.Enums;

namespace Oficina.UnitTests.Application;

public sealed class InventoryAndServiceOrderValidatorsTests
{
    private static readonly Guid Id = Guid.CreateVersion7();

    public static TheoryData<IValidator, object> ValidRequests => new()
    {
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "Filtro", PartType.Part, "un", 25m, 2m) },
        { new UpdatePartCommandValidator(), new UpdatePartCommand(Id, "Filtro", PartType.Supply, "L", 0m, 0.5m) },
        { new RegisterStockEntryCommandValidator(), new RegisterStockEntryCommand(Id, 1.5m, "Compra") },
        { new DeactivatePartCommandValidator(), new DeactivatePartCommand(Id) },
        { new ReactivatePartCommandValidator(), new ReactivatePartCommand(Id) },
        { new GetPartByIdQueryValidator(), new GetPartByIdQuery(Id) },
        { new ListPartsQueryValidator(), new ListPartsQuery(1, 20, false, false) },
        { new OpenServiceOrderCommandValidator(), new OpenServiceOrderCommand(CustomerTestData.MaskedCpf, "BRA2E19", "Barulho ao frear") },
        { new StartDiagnosisCommandValidator(), new StartDiagnosisCommand(Id) },
        { new AddServiceItemCommandValidator(), new AddServiceItemCommand(Id, Id, 1) },
        { new AddPartItemCommandValidator(), new AddPartItemCommand(Id, Id, 0.25m) },
        { new RemoveItemCommandValidator(), new RemoveItemCommand(Id, Id) },
        { new SendQuoteCommandValidator(), new SendQuoteCommand(Id) },
        { new RejectQuoteCommandValidator(), new RejectQuoteCommand(Id, "Muito caro") },
        { new RejectQuoteByTokenCommandValidator(), new RejectQuoteByTokenCommand("token", "Muito caro") },
        { new CompleteServiceOrderCommandValidator(), new CompleteServiceOrderCommand(Id) },
        { new DeliverServiceOrderCommandValidator(), new DeliverServiceOrderCommand(Id) },
    };

    public static TheoryData<IValidator, object, string, string> InvalidRequests => new()
    {
        { new CreatePartCommandValidator(), new CreatePartCommand(" ", "Filtro", PartType.Part, "un", 25m, 2m), "code", "inventory.invalidPartCode" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT 001", "Filtro", PartType.Part, "un", 25m, 2m), "code", "inventory.invalidPartCode" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "F", PartType.Part, "un", 25m, 2m), "name", "inventory.invalidName" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "Filtro", (PartType)9, "un", 25m, 2m), "type", "common.invalidFormat" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "Filtro", PartType.Part, "unidade-longa", 25m, 2m), "unitOfMeasure", "inventory.invalidUnitOfMeasure" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "Filtro", PartType.Part, "un", -1m, 2m), "unitPrice", "common.negativeAmount" },
        { new CreatePartCommandValidator(), new CreatePartCommand("FLT-001", "Filtro", PartType.Part, "un", 25m, 0.0001m), "minimumStock", "inventory.invalidMinimumStock" },
        { new UpdatePartCommandValidator(), new UpdatePartCommand(Guid.Empty, "Filtro", PartType.Part, "un", 25m, 2m), "id", "common.requiredField" },
        { new RegisterStockEntryCommandValidator(), new RegisterStockEntryCommand(Id, 0m, "Compra"), "quantity", "common.invalidQuantity" },
        { new RegisterStockEntryCommandValidator(), new RegisterStockEntryCommand(Id, 1m, " "), "reason", "inventory.invalidReason" },
        { new ListPartsQueryValidator(), new ListPartsQuery(0, 20, false, false), "page", "common.invalidPage" },
        { new OpenServiceOrderCommandValidator(), new OpenServiceOrderCommand("123", "BRA2E19", "Barulho"), "customerDocument", "customer.invalidDocument" },
        { new OpenServiceOrderCommandValidator(), new OpenServiceOrderCommand(CustomerTestData.Cpf, "B", "Barulho"), "licensePlate", "vehicle.invalidLicensePlate" },
        { new OpenServiceOrderCommandValidator(), new OpenServiceOrderCommand(CustomerTestData.Cpf, "BRA2E19", " a "), "problemDescription", "serviceOrder.invalidProblemDescription" },
        { new StartDiagnosisCommandValidator(), new StartDiagnosisCommand(Guid.Empty), "serviceOrderId", "common.requiredField" },
        { new AddServiceItemCommandValidator(), new AddServiceItemCommand(Id, Id, 0), "quantity", "serviceOrder.invalidServiceQuantity" },
        { new AddServiceItemCommandValidator(), new AddServiceItemCommand(Id, Guid.Empty, 1), "serviceId", "common.requiredField" },
        { new AddPartItemCommandValidator(), new AddPartItemCommand(Id, Id, 0.0001m), "quantity", "common.invalidQuantity" },
        { new RemoveItemCommandValidator(), new RemoveItemCommand(Id, Guid.Empty), "itemId", "common.requiredField" },
        { new RejectQuoteCommandValidator(), new RejectQuoteCommand(Id, " "), "reason", "serviceOrder.rejectionReasonRequired" },
        { new RejectQuoteByTokenCommandValidator(), new RejectQuoteByTokenCommand("", "Muito caro"), "token", "serviceOrder.invalidApprovalToken" },
    };

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void Validate_ValidRequest_HasNoErrors(IValidator validator, object request)
    {
        var result = validator.Validate(new ValidationContext<object>(request));

        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(error => error.ErrorCode)));
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Validate_InvalidField_ReturnsTheErrorCode(IValidator validator, object request, string field, string code)
    {
        var failure = Assert.Single(validator.Validate(new ValidationContext<object>(request)).Errors);

        Assert.Equal(field, char.ToLowerInvariant(failure.PropertyName[0]) + failure.PropertyName[1..]);
        Assert.Equal(code, failure.ErrorCode);
    }
}

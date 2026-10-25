using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.ConfirmPixPayment;

public sealed record ConfirmPixPaymentCommand(string? TxId, string? Signature) : IRequest<Result>;

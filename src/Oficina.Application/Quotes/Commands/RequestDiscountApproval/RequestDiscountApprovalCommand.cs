using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.RequestDiscountApproval;

public sealed record RequestDiscountApprovalCommand(Guid Id, string Type, decimal Value, string Reason) : IRequest<Result<DiscountApprovalDto>>;

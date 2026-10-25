using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.DecideDiscountApproval;

public sealed record DecideDiscountApprovalCommand(Guid Id, Guid ApprovalId, bool Approve) : IRequest<Result<DiscountApprovalDto>>;

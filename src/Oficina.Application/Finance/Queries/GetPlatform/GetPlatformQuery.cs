using MediatR;
using Oficina.Application.Finance.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance.Queries.GetPlatform;

public sealed record GetPlatformQuery : IRequest<Result<PlatformDto>>;

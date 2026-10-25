using Oficina.Domain.Identity.Entities;

namespace Oficina.Application.Identity.Invitations;

public sealed record IssuedInvitation(UserInvitation Invitation, string Token);

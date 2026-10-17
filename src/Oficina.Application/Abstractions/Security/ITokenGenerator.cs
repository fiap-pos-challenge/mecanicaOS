using Oficina.Domain.Identity.Entities;

namespace Oficina.Application.Abstractions.Security;

public interface ITokenGenerator
{
    AccessToken Generate(User user, Guid? sessionId = null);
}

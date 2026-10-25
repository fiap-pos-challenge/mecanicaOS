namespace Oficina.Application.Abstractions.Links;

public interface IPublicLinks
{
    string Portal(string token);

    string DocumentVerification(string code);
}

using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Portal.Errors;

public static class PortalErrors
{
    public static readonly Error LinkUnavailable = new("portal.linkUnavailable", "Este link não está mais disponível. Peça um novo à oficina.", ErrorType.NotFound);

    public static readonly Error Locked = new("portal.locked", "Acesso bloqueado após tentativas erradas. Fale com a oficina.", ErrorType.Locked);

    public static readonly Error SessionExpired = new("portal.sessionExpired", "Sua sessão expirou. Abra o link de novo.", ErrorType.Unauthorized);

    public static readonly Error AlreadyAnswered = new("portal.alreadyAnswered", "Este orçamento já foi respondido.", ErrorType.Conflict);

    public static readonly Error NoItems = new("portal.noItems", "Escolha pelo menos um item para aprovar.", ErrorType.Validation,
        [new FieldError("itemIds", "portal.noItems", "Escolha pelo menos um item para aprovar.")]);

    public static readonly Error NothingToPay = new("portal.nothingToPay", "Não há valor a pagar agora.", ErrorType.BusinessRule);

    public static readonly Error PixUnavailable = new("portal.pixUnavailable", "A oficina ainda não cadastrou a chave Pix. Pague no balcão.", ErrorType.BusinessRule);

    public static readonly Error CardUnavailable = new("portal.cardUnavailable", "Pagamento com cartão pelo portal ainda não está disponível. Pague no balcão.", ErrorType.BusinessRule);

    public static readonly Error PaymentNotFound = new("portal.paymentNotFound", "Cobrança não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidWebhook = new("portal.invalidWebhook", "Assinatura inválida.", ErrorType.Unauthorized);

    public static readonly Error DocumentNotFound = new("documents.notFound", "Documento não encontrado.", ErrorType.NotFound);

    public static readonly Error InvalidChannel = new("documents.invalidChannel", "Escolha WhatsApp ou e-mail.", ErrorType.Validation,
        [new FieldError("channel", "documents.invalidChannel", "Escolha WhatsApp ou e-mail.")]);
}

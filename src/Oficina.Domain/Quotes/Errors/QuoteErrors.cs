using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Quotes.Errors;

public static class QuoteErrors
{
    public static readonly Error NotFound = new("quotes.notFound", "Orçamento não encontrado.", ErrorType.NotFound);

    public static readonly Error Locked = new("quotes.locked", "O orçamento já foi enviado ou respondido e não pode ser alterado.", ErrorType.Conflict);

    public static readonly Error AlreadyExists = new("quotes.alreadyExists", "Esta OS já tem orçamento.", ErrorType.Conflict);

    public static readonly Error NotPending = new("quotes.notPending", "O orçamento já foi respondido.", ErrorType.Conflict);

    public static readonly Error VersionNotAllowed = new("quotes.versionNotAllowed", "Só é possível criar nova versão da última versão recusada.", ErrorType.Conflict);

    public static readonly Error RetryNotAllowed = new("quotes.retryNotAllowed", "O envio ainda está válido; não é preciso reenviar.", ErrorType.Conflict);

    public static readonly Error ApprovalDecided = new("quotes.approvalDecided", "O pedido de aprovação já foi decidido.", ErrorType.Conflict);

    public static readonly Error ApprovalNotNeeded = new("quotes.approvalNotNeeded", "O desconto está dentro do seu limite; não precisa de aprovação.", ErrorType.BusinessRule);

    public static readonly Error ApprovalRequired = new("quotes.approvalRequired", "O desconto passou do seu limite e precisa de aprovação do gestor.", ErrorType.BusinessRule);

    public static readonly Error NoConsent = new("quotes.noConsent", "O cliente não autorizou este canal ou não tem o contato cadastrado.", ErrorType.BusinessRule);

    public static readonly Error Empty = new("quotes.empty", "Adicione pelo menos um item ao orçamento.", ErrorType.BusinessRule);

    public static readonly Error ZeroTotal = new("quotes.zeroTotal", "O total do orçamento precisa ser maior que zero.", ErrorType.BusinessRule);

    public static readonly Error NoDelivery = new("quotes.noDelivery", "O orçamento ainda não foi enviado.", ErrorType.NotFound);

    public static readonly Error InvalidItem = new("quotes.invalidItem", "Escolha itens ativos do catálogo.", ErrorType.Validation,
        [new FieldError("items", "quotes.invalidItem", "Escolha itens ativos do catálogo.")]);

    public static readonly Error InvalidQuantity = new("quotes.invalidQuantity", "Quantidade inválida: inteira para serviço e peça, até 3 casas para insumo, no máximo 999.", ErrorType.Validation,
        [new FieldError("items", "quotes.invalidQuantity", "Quantidade inválida: inteira para serviço e peça, até 3 casas para insumo, no máximo 999.")]);

    public static readonly Error InvalidDiscount = new("quotes.invalidDiscount", "Informe um desconto maior que zero (percentual até 100%).", ErrorType.Validation,
        [new FieldError("value", "quotes.invalidDiscount", "Informe um desconto maior que zero (percentual até 100%).")]);

    public static readonly Error DiscountReason = new("quotes.discountReason", "Explique o motivo do desconto com pelo menos 5 caracteres.", ErrorType.Validation,
        [new FieldError("reason", "quotes.discountReason", "Explique o motivo do desconto com pelo menos 5 caracteres.")]);

    public static readonly Error RejectReason = new("quotes.rejectReason", "Informe o motivo com pelo menos 5 caracteres.", ErrorType.Validation,
        [new FieldError("reason", "quotes.rejectReason", "Informe o motivo com pelo menos 5 caracteres.")]);

    public static readonly Error InvalidExpiration = new("quotes.invalidExpiration", "Escolha um prazo de validade da lista.", ErrorType.Validation,
        [new FieldError("expiresInDays", "quotes.invalidExpiration", "Escolha um prazo de validade da lista.")]);

    public static readonly Error InvalidMessage = new("quotes.invalidMessage", "Escreva a mensagem com até 1000 caracteres.", ErrorType.Validation,
        [new FieldError("message", "quotes.invalidMessage", "Escreva a mensagem com até 1000 caracteres.")]);

    public static readonly Error InvalidChannel = new("quotes.invalidChannel", "Escolha WhatsApp, e-mail ou link.", ErrorType.Validation,
        [new FieldError("channel", "quotes.invalidChannel", "Escolha WhatsApp, e-mail ou link.")]);
}

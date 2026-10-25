namespace Oficina.Application.Settings;

public static class SettingsMessages
{
    public const string LogoType = "Envie uma imagem PNG ou JPG.";
    public const string LogoSize = "A imagem precisa ter até 512 KB.";
    public const string RuleName = "Dê um nome com até 80 caracteres e escolha a condição de cada regra.";
    public const string RuleValue = "Informe um valor de desconto maior que zero (até 100%).";
    public const string Limits = "O limite de desconto vai de 0% a 100%.";
    public const string Methods = "Escolha pelo menos uma forma de pagamento.";
    public const string Installments = "Parcelas de 1 a 24; sem juros até o máximo de parcelas.";
    public const string Interest = "Juros ao mês de 0% a 15%.";
    public const string Fees = "Taxa de 0% a 20% e recebimento em até 60 dias.";
    public const string Template = "Modelo de mensagem inválido.";
    public const string TemplateLength = "Escreva cada mensagem com até 1000 caracteres.";
    public const string TemplateVariables = "Use só as variáveis {cliente}, {veiculo}, {placa}, {total}, {link} e {prazo}.";
    public const string Window = "Informe a janela de envio com início antes do fim.";
    public const string IdleMinutes = "Inatividade de 5 a 60 minutos.";
    public const string LockoutAttempts = "Bloqueio depois de 3 a 10 tentativas.";
    public const string LockoutMinutes = "Bloqueio de 5 a 120 minutos.";
    public const string Area = "Área de auditoria inválida.";
    public const string Name = "Informe o nome da oficina com 3 a 120 caracteres.";
    public const string Document = "Informe um CNPJ válido.";
    public const string Address = "Preencha CEP, rua, número, cidade e UF.";
    public const string Hours = "Informe o horário de cada dia com abertura antes do fechamento.";
    public const string System = "Sistema";
    public const string WorkshopChanged = "Dados da oficina alterados";
    public const string LogoChanged = "Logo da oficina alterado";
    public const string DiscountsChanged = "Regras de desconto alteradas";
    public const string PaymentsChanged = "Formas e taxas de pagamento alteradas";
    public const string MessagesChanged = "Modelos de mensagem alterados";
    public const string SecurityChanged = "Política de segurança alterada";
    public const string SessionRevoked = "Sessão encerrada";
    public const string SessionRevokedDetail = "Encerrada pelo administrador";
    public const string TwoFactorRequired = "2FA obrigatório";
    public const string TwoFactorOptional = "2FA opcional";
}

using System.Globalization;
using System.Text.RegularExpressions;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Audit.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings;

public sealed partial class SettingsService(
    SettingsStore store,
    ReauthenticationGuard guard,
    ISettingsQueries queries,
    IAuditRepository audit,
    IUserSessionRepository sessions,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const int MaxLogoBytes = 512 * 1024;
    public const int MaxTemplateLength = 1000;
    public static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    public static readonly string[] PaymentMethods = ["Pix", "Credit", "Debit", "Cash", "Boleto"];
    public static readonly string[] Roles = ["Admin", "Manager", "Attendant", "Mechanic"];
    public static readonly string[] MessageKinds = ["QuoteSent", "StatusChanged", "QuoteReminder", "Receipt"];
    public static readonly string[] Variables = ["cliente", "veiculo", "placa", "total", "link", "prazo"];
    private static readonly string[] Conditions = ["LoyalCustomer", "FirstService", "Fleet"];
    private static readonly string[] AuditAreas = ["discounts", "payments", "security", "customers", "stock"];

    public async Task<Result<WorkshopSettings>> WorkshopAsync(CancellationToken cancellationToken)
    {
        var workshop = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        return workshop with { Plan = workshop.Plan with { Users = await queries.ActiveUsersAsync(cancellationToken) } };
    }

    public async Task<Result<WorkshopSettings>> SaveWorkshopAsync(string? reauthentication, WorkshopInput input, CancellationToken cancellationToken)
    {
        if (await ValidateWorkshop(input) is { } invalid)
        {
            return invalid;
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        var address = input.Address with
        {
            ZipCode = Digits(input.Address.ZipCode),
            Street = input.Address.Street.Trim(),
            Number = input.Address.Number.Trim(),
            District = input.Address.District.Trim(),
            City = input.Address.City.Trim(),
            State = input.Address.State.Trim().ToUpperInvariant(),
        };
        var saved = current with { Name = input.Name.Trim(), Document = Digits(input.Document), Address = address, Hours = Weekdays.Select(day => input.Hours.First(hour => hour.Weekday == day)).ToList() };
        await store.SaveAsync(SettingsKeys.Workshop, saved, cancellationToken);
        await RecordAsync("security", "Dados da oficina alterados", saved.Name, string.Empty, cancellationToken);
        return await CommitAsync(saved with { Plan = saved.Plan with { Users = await queries.ActiveUsersAsync(cancellationToken) } }, cancellationToken);
    }

    public async Task<Result<LogoDto>> SaveLogoAsync(string? reauthentication, LogoInput input, CancellationToken cancellationToken)
    {
        if (input.ContentType is not ("image/png" or "image/jpeg") || !input.DataUrl.StartsWith($"data:{input.ContentType};base64,", StringComparison.Ordinal))
        {
            return SettingsErrors.Invalid("logo", "Envie uma imagem PNG ou JPG.");
        }

        if ((input.DataUrl.Length - input.DataUrl.IndexOf(',', StringComparison.Ordinal) - 1) * 3 / 4 > MaxLogoBytes)
        {
            return SettingsErrors.Invalid("logo", "A imagem precisa ter até 512 KB.");
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        await store.SaveAsync(SettingsKeys.Workshop, current with { LogoUrl = input.DataUrl }, cancellationToken);
        await RecordAsync("security", "Logo da oficina alterado", current.Name, string.Empty, cancellationToken);
        return await CommitAsync(new LogoDto(input.DataUrl), cancellationToken);
    }

    public async Task<Result<DiscountSettings>> DiscountsAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);

    public async Task<Result<DiscountSettings>> SaveDiscountsAsync(string? reauthentication, DiscountSettings input, CancellationToken cancellationToken)
    {
        var rules = input.Rules ?? [];
        var limits = input.Limits ?? [];
        if (rules.Any(rule => string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Trim().Length > 80 || !Conditions.Contains(rule.Condition)))
        {
            return SettingsErrors.Invalid("ruleName", "Dê um nome com até 80 caracteres e escolha a condição de cada regra.");
        }

        if (rules.Any(rule => rule.Value <= 0 || (rule.Type == "Percent" && rule.Value > 100) || rule.Type is not ("Percent" or "Amount")))
        {
            return SettingsErrors.Invalid("ruleValue", "Informe um valor de desconto maior que zero (até 100%).");
        }

        if (limits.Any(limit => !Roles.Contains(limit.Role) || limit.Percent < 0 || limit.Percent > 100))
        {
            return SettingsErrors.Invalid("limits", "O limite de desconto vai de 0% a 100%.");
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);
        var saved = new DiscountSettings(
            rules.Select(rule => rule with { Id = rule.Id == Guid.Empty ? Guid.CreateVersion7() : rule.Id, Name = rule.Name.Trim() }).ToList(),
            Roles.Select(role => limits.FirstOrDefault(limit => limit.Role == role) ?? current.Limits.FirstOrDefault(limit => limit.Role == role) ?? new DiscountLimitSettings(role, 0)).ToList());
        await store.SaveAsync(SettingsKeys.Discounts, saved, cancellationToken);
        await RecordAsync("discounts", "Regras de desconto alteradas", $"{saved.Rules.Count} regras", string.Join(", ", saved.Limits.Select(limit => $"{limit.Role} {limit.Percent.ToString("0.##", CultureInfo.InvariantCulture)}%")), cancellationToken);
        return await CommitAsync(saved, cancellationToken);
    }

    public async Task<Result<PaymentSettings>> PaymentsAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);

    public async Task<Result<PaymentSettings>> SavePaymentsAsync(string? reauthentication, PaymentInput input, CancellationToken cancellationToken)
    {
        var methods = (input.Methods ?? []).Distinct().ToList();
        if (methods.Count == 0 || methods.Any(method => !PaymentMethods.Contains(method)))
        {
            return SettingsErrors.Invalid("methods", "Escolha pelo menos uma forma de pagamento.");
        }

        if (input.MaxInstallments is < 1 or > 24 || input.InterestFreeInstallments < 1 || input.InterestFreeInstallments > input.MaxInstallments)
        {
            return SettingsErrors.Invalid("installments", "Parcelas de 1 a 24; sem juros até o máximo de parcelas.");
        }

        if (input.MonthlyInterestPercent is < 0 or > 15)
        {
            return SettingsErrors.Invalid("interest", "Juros ao mês de 0% a 15%.");
        }

        var fees = input.Fees ?? [];
        if (fees.Any(fee => !PaymentMethods.Contains(fee.Method) || fee.FeePercent < 0 || fee.FeePercent > 20 || fee.SettlementDays is < 0 or > 60))
        {
            return SettingsErrors.Invalid("fees", "Taxa de 0% a 20% e recebimento em até 60 dias.");
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
        var saved = new PaymentSettings(methods, input.MaxInstallments, input.InterestFreeInstallments, input.MonthlyInterestPercent, fees.ToList(), input.PixKey is null ? current.PixKey : NullIfEmpty(input.PixKey));
        await store.SaveAsync(SettingsKeys.Payments, saved, cancellationToken);
        await RecordAsync("payments", "Formas e taxas de pagamento alteradas", string.Join(", ", methods), string.Empty, cancellationToken);
        return await CommitAsync(saved, cancellationToken);
    }

    public async Task<Result<MessageSettings>> MessagesAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Messages, SettingsDefaults.Messages, cancellationToken);

    public async Task<Result<MessageSettings>> SaveMessagesAsync(string? reauthentication, MessageSettings input, CancellationToken cancellationToken)
    {
        var templates = input.Templates ?? [];
        if (templates.Any(template => !MessageKinds.Contains(template.Kind) || template.Channel is not ("WhatsApp" or "Email")))
        {
            return SettingsErrors.Invalid("templates", "Modelo de mensagem inválido.");
        }

        if (templates.Any(template => string.IsNullOrWhiteSpace(template.Body) || template.Body.Length > MaxTemplateLength))
        {
            return SettingsErrors.Invalid("templates", "Escreva cada mensagem com até 1000 caracteres.");
        }

        if (templates.Any(template => VariablePattern().Matches(template.Body).Any(match => !Variables.Contains(match.Groups[1].Value))))
        {
            return SettingsErrors.Invalid("templates", "Use só as variáveis {cliente}, {veiculo}, {placa}, {total}, {link} e {prazo}.");
        }

        if (input.Window is not { } window || !IsTime(window.From) || !IsTime(window.To) || string.CompareOrdinal(window.From, window.To) >= 0 || (window.Weekdays ?? []).Any(day => !Weekdays.Contains(day)))
        {
            return SettingsErrors.Invalid("window", "Informe a janela de envio com início antes do fim.");
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var saved = new MessageSettings(templates.Select(template => template with { Body = template.Body.Trim() }).ToList(), window with { Weekdays = Weekdays.Where(day => (window.Weekdays ?? []).Contains(day)).ToList() });
        await store.SaveAsync(SettingsKeys.Messages, saved, cancellationToken);
        await RecordAsync("security", "Modelos de mensagem alterados", $"{saved.Templates.Count} modelos", string.Empty, cancellationToken);
        return await CommitAsync(saved, cancellationToken);
    }

    public async Task<Result<SecuritySettings>> SecurityAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Security, SettingsDefaults.Security, cancellationToken);

    public async Task<Result<SecuritySettings>> SaveSecurityAsync(string? reauthentication, SecuritySettings input, CancellationToken cancellationToken)
    {
        if (input.IdleMinutes is < 5 or > 60)
        {
            return SettingsErrors.Invalid("idleMinutes", "Inatividade de 5 a 60 minutos.");
        }

        if (input.LockoutAttempts is < 3 or > 10)
        {
            return SettingsErrors.Invalid("lockoutAttempts", "Bloqueio depois de 3 a 10 tentativas.");
        }

        if (input.LockoutMinutes is < 5 or > 120)
        {
            return SettingsErrors.Invalid("lockoutMinutes", "Bloqueio de 5 a 120 minutos.");
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        await store.SaveAsync(SettingsKeys.Security, input, cancellationToken);
        await RecordAsync("security", "Política de segurança alterada", $"{input.IdleMinutes} min de inatividade", input.RequireTwoFactorForAll ? "2FA obrigatório" : "2FA opcional", cancellationToken);
        return await CommitAsync(input, cancellationToken);
    }

    public async Task<Result<SessionListDto>> SessionsAsync(CancellationToken cancellationToken) =>
        new SessionListDto(await queries.SessionsAsync(currentUser.SessionId, timeProvider.GetUtcNow(), cancellationToken));

    public async Task<Result> RevokeSessionAsync(string? reauthentication, Guid id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (id == currentUser.SessionId || await sessions.GetByIdAsync(id, cancellationToken) is not { } session || !session.IsActive(now))
        {
            return SettingsErrors.SessionNotFound;
        }

        if ((await guard.ConsumeAsync(reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        session.Revoke(now);
        await RecordAsync("security", "Sessão encerrada", session.Device, "Encerrada pelo administrador", cancellationToken);
        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result<PagedResult<AuditEntryDto>>> AuditAsync(AuditFilter filter, CancellationToken cancellationToken)
    {
        var area = string.IsNullOrWhiteSpace(filter.Area) || filter.Area == "all" ? null : filter.Area;
        if (area is not null && !AuditAreas.Contains(area))
        {
            return SettingsErrors.Invalid("area", "Área de auditoria inválida.");
        }

        var page = Math.Max(PageRequest.FirstPage, filter.Page);
        var size = filter.PageSize is < 1 or > PageRequest.MaxPageSize ? PageRequest.DefaultPageSize : filter.PageSize;
        return await queries.AuditAsync(filter with { Area = area, Page = page, PageSize = size, Actor = filter.Actor?.Trim() }, cancellationToken);
    }

    public async Task<Result<FiscalSettings>> FiscalAsync(CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Fiscal, SettingsDefaults.Fiscal, cancellationToken);

    public async Task RecordAsync(string area, string action, string target, string detail, CancellationToken cancellationToken)
    {
        var actor = currentUser.UserId is { } userId ? await users.GetByIdAsync(userId, cancellationToken) : null;
        audit.Add(AuditEntry.Record(timeProvider.GetUtcNow(), actor?.Id, actor?.Name ?? "Sistema", area, action, target, detail));
    }

    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex VariablePattern();

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex TimePattern();

    private static bool IsTime(string? value) => value is not null && TimePattern().IsMatch(value);

    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Task<Error?> ValidateWorkshop(WorkshopInput input)
    {
        Error? error = null;
        var name = input.Name?.Trim() ?? string.Empty;
        if (name.Length is < 3 or > 120)
        {
            error = SettingsErrors.Invalid("name", "Informe o nome da oficina com 3 a 120 caracteres.");
        }
        else if (Document.Create(input.Document ?? string.Empty) is not { IsSuccess: true } document || document.Value.Type != Domain.Customers.Enums.DocumentType.Cnpj)
        {
            error = SettingsErrors.Invalid("document", "Informe um CNPJ válido.");
        }
        else if (input.Address is not { } address || Digits(address.ZipCode).Length != 8 || string.IsNullOrWhiteSpace(address.Street) || string.IsNullOrWhiteSpace(address.Number)
            || string.IsNullOrWhiteSpace(address.City) || (address.State?.Trim().Length ?? 0) != 2)
        {
            error = SettingsErrors.Invalid("address", "Preencha CEP, rua, número, cidade e UF.");
        }
        else if (input.Hours is null || Weekdays.Any(day => !input.Hours.Any(hour => hour.Weekday == day))
            || input.Hours.Any(hour => !hour.Closed && (!IsTime(hour.Opens) || !IsTime(hour.Closes) || string.CompareOrdinal(hour.Opens, hour.Closes) >= 0)))
        {
            error = SettingsErrors.Invalid("hours", "Informe o horário de cada dia com abertura antes do fechamento.");
        }

        return Task.FromResult(error);
    }

    private async Task<Result<T>> CommitAsync<T>(T value, CancellationToken cancellationToken) =>
        (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : value;
}

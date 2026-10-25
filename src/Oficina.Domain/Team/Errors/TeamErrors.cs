using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Team.Errors;

public static class TeamErrors
{
    public static readonly Error InvalidSpecialties = new(
        "team.invalidSpecialties",
        "Informe até 8 especialidades, cada uma com 2 a 40 caracteres.",
        ErrorType.Validation,
        [new FieldError("specialties", "team.invalidSpecialties", "Informe até 8 especialidades, cada uma com 2 a 40 caracteres.")]);

    public static readonly Error InvalidCommission = new(
        "team.invalidCommission",
        "A comissão deve ficar entre 0% e 50%.",
        ErrorType.Validation,
        [new FieldError("commissionPercent", "team.invalidCommission", "A comissão deve ficar entre 0% e 50%.")]);

    public static readonly Error InvalidDiscountLimit = new(
        "team.invalidDiscountLimit",
        "O limite de desconto deve ficar entre 0% e 30%.",
        ErrorType.Validation,
        [new FieldError("discountLimitPercent", "team.invalidDiscountLimit", "O limite de desconto deve ficar entre 0% e 30%.")]);

    public static readonly Error InvalidShift = new(
        "team.invalidShift",
        "Cada dia da jornada precisa de início antes do fim.",
        ErrorType.Validation,
        [new FieldError("schedule", "team.invalidShift", "Cada dia da jornada precisa de início antes do fim.")]);

    public static readonly Error MechanicDiscount = new(
        "team.mechanicDiscount",
        "Mecânico não aplica desconto: o limite deve ser 0%.",
        ErrorType.BusinessRule);

    public static readonly Error InvalidPeriod = new(
        "team.invalidPeriod",
        "Escolha o período de 30 ou 90 dias.",
        ErrorType.Validation);

    public static readonly Error TooManyUsers = new(
        "team.tooManyUsers",
        "Consulte até 500 pessoas por vez.",
        ErrorType.Validation);

    public static readonly Error RolesNotCustomizable = new(
        "roles.customizationUnavailable",
        "A personalização de perfis ainda não está disponível.",
        ErrorType.BusinessRule);
}

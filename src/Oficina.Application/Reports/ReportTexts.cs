namespace Oficina.Application.Reports;

public static class ReportTexts
{
    public const string ExportReadyTitle = "Exportação de relatório pronta";
    public const string ScheduleInvalid = "Revise o agendamento.";
    public const string Weekday = "Escolha o dia da semana.";
    public const string Time = "Informe o horário no formato HH:mm.";
    public const string Recipients = "Informe de 1 a 10 e-mails válidos.";
    public const string Format = "Escolha Excel (xlsx) ou PDF.";

    public static string ScheduledSubject(string name) => $"Relatório agendado: {name}";

    public static string ExportReadyBody(string explanation, string format) => $"{explanation} ({format}).";

    public static string FileName(string format) => $"relatorio.{format}";
}

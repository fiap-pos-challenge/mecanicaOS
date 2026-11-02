namespace Oficina.Api.ExceptionHandling;

public static class ProblemTitles
{
    public static string For(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Requisição inválida",
        StatusCodes.Status401Unauthorized => "Não autenticado",
        StatusCodes.Status403Forbidden => "Acesso negado",
        StatusCodes.Status404NotFound => "Recurso não encontrado",
        StatusCodes.Status409Conflict => "Conflito",
        StatusCodes.Status429TooManyRequests => "Muitas requisições",
        StatusCodes.Status422UnprocessableEntity => "Regra de negócio não atendida",
        _ => "Erro interno",
    };
}

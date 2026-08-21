using System.Diagnostics;
using Korp.SharedKernel.Excecoes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Korp.SharedKernel.Web;

public sealed class TratadorGlobalDeExcecoes(ILogger<TratadorGlobalDeExcecoes> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excecao,
        CancellationToken cancelamento)
    {
        var (status, titulo) = excecao switch
        {
            ExcecaoServicoIndisponivel => (StatusCodes.Status503ServiceUnavailable,"Serviço indisponível"),
            ExcecaoNaoEncontrado => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ExcecaoConflito => (StatusCodes.Status409Conflict, "Conflito de estado"),
            ExcecaoRegraDeNegocio => (StatusCodes.Status400BadRequest, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        var identificador = Activity.Current?.Id ?? contexto.TraceIdentifier;

        if (status is StatusCodes.Status500InternalServerError)
            logger.LogError(excecao, "Erro não tratado em {Caminho} [{Id}]",
                contexto.Request.Path, identificador);
        else
            logger.LogInformation("Requisição rejeitada em {Caminho}: {Mensagem}",
                contexto.Request.Path, excecao.Message);

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = status is StatusCodes.Status500InternalServerError
                ? "Ocorreu um erro inesperado. Tente novamente em instantes."
                : excecao.Message,
            Instance = $"{contexto.Request.Method} {contexto.Request.Path}"
        };
        problema.Extensions["traceId"] = identificador;

        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(problema, cancelamento);
        return true;
    }
}
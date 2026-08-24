using System.Net;
using System.Net.Http.Json;
using Korp.Faturamento.Application.Integracao;
using Korp.SharedKernel.Excecoes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Korp.Faturamento.Infrastructure.Integracao;

public sealed class EstoqueHttpClient(
    HttpClient cliente,
    ILogger<EstoqueHttpClient> registrador) : IEstoqueClient
{
    public async Task<ProdutoEstoque> ObterProdutoAsync(
        Guid produtoId, CancellationToken cancelamento)
    {
        var resposta = await EnviarAsync(
            () => cliente.GetAsync($"/api/produtos/{produtoId}", cancelamento),
            cancelamento);

        if (resposta.StatusCode is HttpStatusCode.NotFound)
            throw new ExcecaoNaoEncontrado(
                $"Produto {produtoId} não existe no serviço de estoque.");

        await GarantirSucessoAsync(resposta, cancelamento);

        return await resposta.Content.ReadFromJsonAsync<ProdutoEstoque>(cancelamento)
               ?? throw new ExcecaoServicoIndisponivel(
                   "O serviço de estoque devolveu uma resposta vazia.");
    }

    public async Task BaixarAsync(
        BaixaEstoqueRequisicao requisicao, CancellationToken cancelamento)
    {
        var resposta = await EnviarAsync(
            () => cliente.PostAsJsonAsync("/api/estoque/baixas", requisicao, cancelamento),
            cancelamento);

        await GarantirSucessoAsync(resposta, cancelamento);
    }

    public async Task<ResumoEstoque?> ObterResumoAsync(CancellationToken cancelamento)
    {
        try
        {
            var resposta = await cliente.GetAsync("/api/produtos/resumo", cancelamento);

            if (!resposta.IsSuccessStatusCode)
            {
                registrador.LogWarning(
                    "Estoque respondeu {Status} no resumo; dashboard seguirá parcial.",
                    (int)resposta.StatusCode);
                return null;
            }

            return await resposta.Content
                .ReadFromJsonAsync<ResumoEstoque>(cancelamento);
        }
        catch (Exception excecao)
        {
            registrador.LogWarning(excecao,
                "Estoque indisponível para o dashboard; seguindo parcial.");
            return null;
        }
    }

    /// <summary>
    /// Converte falhas de transporte em ExcecaoServicoIndisponivel.
    /// O que chega aqui já passou por retry, timeout e circuit breaker.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarAsync(
        Func<Task<HttpResponseMessage>> envio, CancellationToken cancelamento)
    {
        try
        {
            return await envio();
        }
        catch (BrokenCircuitException)
        {
            registrador.LogWarning("Circuito aberto para o serviço de estoque.");
            throw new ExcecaoServicoIndisponivel(
                "O serviço de estoque está indisponível no momento. " +
                "Tente novamente em alguns instantes.");
        }
        catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested)
        {
            registrador.LogWarning("Tempo esgotado ao chamar o serviço de estoque.");
            throw new ExcecaoServicoIndisponivel(
                "O serviço de estoque demorou demais para responder. " +
                "Nenhuma alteração foi feita.");
        }
        catch (TimeoutRejectedException)
        {
            registrador.LogWarning("Timeout ao chamar o serviço de estoque.");
            throw new ExcecaoServicoIndisponivel(
                "O serviço de estoque demorou demais para responder. " +
                "Nenhuma alteração foi feita.");
        }
        catch (HttpRequestException excecao)
        {
            registrador.LogWarning(excecao, "Falha de rede ao chamar o serviço de estoque.");
            throw new ExcecaoServicoIndisponivel(
                "Não foi possível contatar o serviço de estoque. " +
                "Nenhuma alteração foi feita.");
        }
    }

    /// <summary>
    /// Traduz o erro de negócio devolvido pelo Estoque em exceção local,
    /// preservando a mensagem original para o usuário.
    /// </summary>
    private static async Task GarantirSucessoAsync(
        HttpResponseMessage resposta, CancellationToken cancelamento)
    {
        if (resposta.IsSuccessStatusCode) return;

        var problema = await LerProblemaAsync(resposta, cancelamento);

        throw resposta.StatusCode switch
        {
            HttpStatusCode.Conflict => new ExcecaoConflito(problema),
            HttpStatusCode.NotFound => new ExcecaoNaoEncontrado(problema),
            HttpStatusCode.BadRequest => new ExcecaoRegraDeNegocio(problema),
            _ => new ExcecaoServicoIndisponivel(
                "O serviço de estoque está indisponível no momento. " +
                "Nenhuma alteração foi feita.")
        };
    }

    private static async Task<string> LerProblemaAsync(
        HttpResponseMessage resposta, CancellationToken cancelamento)
    {
        try
        {
            var problema = await resposta.Content
                .ReadFromJsonAsync<ProblemDetails>(cancelamento);

            return string.IsNullOrWhiteSpace(problema?.Detail)
                ? "O serviço de estoque recusou a operação."
                : problema.Detail;
        }
        catch
        {
            return "O serviço de estoque recusou a operação.";
        }
    }
}
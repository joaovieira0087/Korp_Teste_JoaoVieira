using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Korp.SharedKernel.Ia;

public sealed class ClienteOpenAi(
    HttpClient cliente,
    IOptions<OpcoesIa> opcoes,
    ILogger<ClienteOpenAi> registrador) : IClienteIa
{
    private readonly OpcoesIa _opcoes = opcoes.Value;

    public bool Disponivel => _opcoes.Habilitada;

    public async Task<string?> GerarTextoAsync(
        string instrucao, string entrada, CancellationToken cancelamento)
    {
        if (!Disponivel)
        {
            registrador.LogDebug("IA não configurada; usando fallback.");
            return null;
        }

        try
        {
            var requisicao = new RequisicaoChat(
                _opcoes.Modelo,
                [
                    new MensagemChat("system", instrucao),
                    new MensagemChat("user", entrada)
                ],
                Temperatura: 0.3,
                MaximoTokens: 500);

            var resposta = await cliente.PostAsJsonAsync(
                "chat/completions", requisicao, cancelamento);

            if (!resposta.IsSuccessStatusCode)
            {
                registrador.LogWarning(
                    "IA respondeu {Status}; usando fallback.", (int)resposta.StatusCode);
                return null;
            }

            var conteudo = await resposta.Content
                .ReadFromJsonAsync<RespostaChat>(cancelamento);

            var texto = conteudo?.Escolhas?.FirstOrDefault()?.Mensagem?.Conteudo;

            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
        catch (Exception excecao)
        {
            registrador.LogWarning(excecao, "Falha ao chamar a IA; usando fallback.");
            return null;
        }
    }

    private sealed record MensagemChat(
        [property: JsonPropertyName("role")] string Papel,
        [property: JsonPropertyName("content")] string Conteudo);

    private sealed record RequisicaoChat(
        [property: JsonPropertyName("model")] string Modelo,
        [property: JsonPropertyName("messages")] IReadOnlyList<MensagemChat> Mensagens,
        [property: JsonPropertyName("temperature")] double Temperatura,
        [property: JsonPropertyName("max_tokens")] int MaximoTokens);

    private sealed record RespostaChat(
        [property: JsonPropertyName("choices")] IReadOnlyList<Escolha>? Escolhas);

    private sealed record Escolha(
        [property: JsonPropertyName("message")] MensagemChat? Mensagem);
}
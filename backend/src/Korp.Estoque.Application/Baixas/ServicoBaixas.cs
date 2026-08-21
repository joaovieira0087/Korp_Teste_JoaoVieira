using System.Text.Json;
using Korp.Estoque.Application.Comum;
using Korp.Estoque.Application.Produtos;
using Korp.SharedKernel.Excecoes;

namespace Korp.Estoque.Application.Baixas;

public sealed class ServicoBaixas(
    IProdutoRepositorio repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<BaixaResposta> ExecutarAsync(
        BaixaRequisicao requisicao, CancellationToken cancelamento)
    {
        if (requisicao.Itens is null || requisicao.Itens.Count == 0)
            throw new ExcecaoRegraDeNegocio("A baixa precisa de ao menos um item.");

        var jaProcessada = await repositorio
            .ObterBaixaProcessadaAsync(requisicao.NotaFiscalId, cancelamento);

        if (jaProcessada is not null)
            return Desserializar(jaProcessada);

        await using var transacao =
            await unidadeDeTrabalho.IniciarTransacaoAsync(cancelamento);

        var identificadores = requisicao.Itens
            .Select(i => i.ProdutoId).Distinct().ToList();

        var produtos = await repositorio
            .ObterPorIdsComBloqueioAsync(identificadores, cancelamento);

        var faltantes = identificadores
            .Where(id => produtos.All(p => p.Id != id))
            .ToList();

        if (faltantes.Count > 0)
            throw new ExcecaoNaoEncontrado(
                $"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}.");

        var itens = new List<ItemBaixaResposta>();

        foreach (var item in requisicao.Itens)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            var saldoAnterior = produto.Saldo;

            produto.Debitar(item.Quantidade);

            itens.Add(new ItemBaixaResposta(
                produto.Id, produto.Codigo, saldoAnterior, produto.Saldo));
        }

        var resposta = new BaixaResposta(requisicao.NotaFiscalId, itens);

        await repositorio.RegistrarBaixaAsync(
            new BaixaProcessada(
                requisicao.NotaFiscalId, JsonSerializer.Serialize(resposta)),
            cancelamento);

        await repositorio.SalvarAlteracoesAsync(cancelamento);
        await transacao.ConfirmarAsync(cancelamento);

        return resposta;
    }

    private static BaixaResposta Desserializar(BaixaProcessada baixa)
        => JsonSerializer.Deserialize<BaixaResposta>(baixa.RespostaJson)
           ?? throw new ExcecaoConflito(
               $"A baixa da nota {baixa.NotaFiscalId} já foi processada.");
}
using Korp.Estoque.Application.Produtos;
using Korp.SharedKernel.Excecoes;

namespace Korp.Estoque.Application.Baixas;

public sealed class ServicoBaixas(IProdutoRepositorio repositorio)
{
    public async Task<BaixaResposta> ExecutarAsync(
        BaixaRequisicao requisicao, CancellationToken cancelamento)
    {
        if (requisicao.Itens is null || requisicao.Itens.Count == 0)
            throw new ExcecaoRegraDeNegocio("A baixa precisa de ao menos um item.");

        var identificadores = requisicao.Itens
            .Select(i => i.ProdutoId)
            .Distinct()
            .ToList();

        var produtos = await repositorio.ObterPorIdsAsync(identificadores, cancelamento);

        var faltantes = identificadores
            .Where(id => produtos.All(p => p.Id != id))
            .ToList();

        if (faltantes.Count > 0)
            throw new ExcecaoNaoEncontrado(
                $"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}.");

        var resultado = new List<ItemBaixaResposta>();

        // Se qualquer item dar erro, a exceção sobe antes do SaveChanges e nada é gravado.cfica so na memoria

        foreach (var item in requisicao.Itens)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            var saldoAnterior = produto.Saldo;

            produto.Debitar(item.Quantidade);

            resultado.Add(new ItemBaixaResposta(
                produto.Id, produto.Codigo, saldoAnterior, produto.Saldo));
        }

        await repositorio.SalvarAlteracoesAsync(cancelamento);

        return new BaixaResposta(requisicao.NotaFiscalId, resultado);
    }
}
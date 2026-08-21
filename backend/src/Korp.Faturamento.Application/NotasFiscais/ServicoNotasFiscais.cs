using Korp.Faturamento.Application.Integracao;
using Korp.SharedKernel.Excecoes;

namespace Korp.Faturamento.Application.NotasFiscais;

public sealed class ServicoNotasFiscais(
    INotaFiscalRepositorio repositorio,
    IEstoqueClient estoqueClient)
{
    public async Task<NotaFiscalResposta> CriarAsync(
        CriarNotaFiscalRequisicao requisicao, CancellationToken cancelamento)
    {
        var numero = await repositorio.ProximoNumeroAsync(cancelamento);
        var nota = new NotaFiscal(numero);

        foreach (var item in requisicao.Itens ?? [])
            await AplicarItemAsync(nota, item, cancelamento);

        await repositorio.AdicionarAsync(nota, cancelamento);
        await repositorio.SalvarAlteracoesAsync(cancelamento);

        return Mapear(nota);
    }

    public async Task<IReadOnlyList<NotaFiscalResposta>> ListarAsync(
        StatusNotaFiscal? status, CancellationToken cancelamento)
    {
        var notas = await repositorio.ListarAsync(status, cancelamento);
        return notas.Select(Mapear).ToList();
    }

    public async Task<NotaFiscalResposta> ObterPorIdAsync(
        Guid id, CancellationToken cancelamento)
        => Mapear(await BuscarOuFalharAsync(id, cancelamento));

    public async Task<NotaFiscalResposta> AdicionarItemAsync(
        Guid id, ItemRequisicao item, CancellationToken cancelamento)
    {
        var nota = await BuscarOuFalharAsync(id, cancelamento);

        await AplicarItemAsync(nota, item, cancelamento);

        await repositorio.SalvarAlteracoesAsync(cancelamento);
        return Mapear(nota);
    }

    public async Task<NotaFiscalResposta> RemoverItemAsync(
        Guid id, Guid produtoId, CancellationToken cancelamento)
    {
        var nota = await BuscarOuFalharAsync(id, cancelamento);

        nota.RemoverItem(produtoId);

        await repositorio.SalvarAlteracoesAsync(cancelamento);
        return Mapear(nota);
    }

    public async Task<NotaFiscalResposta> ImprimirAsync(
        Guid id, CancellationToken cancelamento)
    {
        var nota = await BuscarOuFalharAsync(id, cancelamento);

        if (nota.Status is not StatusNotaFiscal.Aberta)
            throw new ExcecaoConflito(
                $"A nota fiscal {nota.Numero} já foi impressa e está fechada.");

        if (nota.Itens.Count == 0)
            throw new ExcecaoRegraDeNegocio(
                "Não é possível imprimir uma nota fiscal sem itens.");

        var baixa = new BaixaEstoqueRequisicao(
            nota.Id,
            nota.Itens.Select(i => new ItemBaixa(i.ProdutoId, i.Quantidade)).ToList());

        // Primeiro tenta baixar o estoque. Se falhar, a exceção sobe e a nota permanece Aberta.
        await estoqueClient.BaixarAsync(baixa, cancelamento);

        nota.Fechar();
        await repositorio.SalvarAlteracoesAsync(cancelamento);

        return Mapear(nota);
    }

    private async Task AplicarItemAsync(
        NotaFiscal nota, ItemRequisicao item, CancellationToken cancelamento)
    {
        var produto = await estoqueClient.ObterProdutoAsync(item.ProdutoId, cancelamento);

        nota.AdicionarItem(
            produto.Id, produto.Codigo, produto.Descricao, item.Quantidade);
    }

    private async Task<NotaFiscal> BuscarOuFalharAsync(
        Guid id, CancellationToken cancelamento)
        => await repositorio.ObterPorIdAsync(id, cancelamento)
           ?? throw new ExcecaoNaoEncontrado($"Nota fiscal {id} não encontrada.");

    private static NotaFiscalResposta Mapear(NotaFiscal nota)
        => new(
            nota.Id,
            nota.Numero,
            nota.Status.ToString(),
            nota.CriadaEm,
            nota.FechadaEm,
            nota.Itens
                .OrderBy(i => i.CodigoProduto)
                .Select(i => new ItemResposta(
                    i.ProdutoId, i.CodigoProduto, i.DescricaoProduto, i.Quantidade))
                .ToList(),
            nota.Itens.Sum(i => i.Quantidade));
}
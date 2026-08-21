using Korp.SharedKernel.Excecoes;

namespace Korp.Faturamento.Application.NotasFiscais;

public sealed class ServicoNotasFiscais(INotaFiscalRepositorio repositorio)
{
    public async Task<NotaFiscalResposta> CriarAsync(
        CriarNotaFiscalRequisicao requisicao, CancellationToken cancelamento)
    {
        var numero = await repositorio.ProximoNumeroAsync(cancelamento);
        var nota = new NotaFiscal(numero);

        foreach (var item in requisicao.Itens ?? [])
            nota.AdicionarItem(
                item.ProdutoId, item.CodigoProduto, item.DescricaoProduto, item.Quantidade);

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

        nota.AdicionarItem(
            item.ProdutoId, item.CodigoProduto, item.DescricaoProduto, item.Quantidade);

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
                .Select(i => new ItemResposta(
                    i.ProdutoId, i.CodigoProduto, i.DescricaoProduto, i.Quantidade))
                .ToList(),
            nota.Itens.Sum(i => i.Quantidade));
}
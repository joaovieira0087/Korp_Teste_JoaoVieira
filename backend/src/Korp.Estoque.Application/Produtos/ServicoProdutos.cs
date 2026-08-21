using Korp.SharedKernel.Excecoes;

namespace Korp.Estoque.Application.Produtos;

public sealed class ServicoProdutos(IProdutoRepositorio repositorio)
{
    public async Task<IReadOnlyList<ProdutoResposta>> ListarAsync(
        string? filtro, CancellationToken cancelamento)
    {
        var produtos = await repositorio.ListarAsync(filtro, cancelamento);
        return produtos.Select(Mapear).ToList();
    }

    public async Task<ProdutoResposta> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
        => Mapear(await BuscarOuFalharAsync(id, cancelamento));

    public async Task<ProdutoResposta> CriarAsync(
        CriarProdutoRequisicao requisicao, CancellationToken cancelamento)
    {
        var codigo = requisicao.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;

        if (await repositorio.ExisteCodigoAsync(codigo, cancelamento))
            throw new ExcecaoConflito($"Já existe um produto com o código '{codigo}'.");

        var produto = new Produto(requisicao.Codigo!, requisicao.Descricao, requisicao.Saldo);

        await repositorio.AdicionarAsync(produto, cancelamento);
        await repositorio.SalvarAlteracoesAsync(cancelamento);

        return Mapear(produto);
    }

    public async Task<ProdutoResposta> AtualizarAsync(
        Guid id, AtualizarProdutoRequisicao requisicao, CancellationToken cancelamento)
    {
        var produto = await BuscarOuFalharAsync(id, cancelamento);

        produto.AlterarDescricao(requisicao.Descricao);
        produto.AjustarSaldo(requisicao.Saldo);

        await repositorio.SalvarAlteracoesAsync(cancelamento);
        return Mapear(produto);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancelamento)
    {
        var produto = await BuscarOuFalharAsync(id, cancelamento);
        repositorio.Remover(produto);
        await repositorio.SalvarAlteracoesAsync(cancelamento);
    }

    private async Task<Produto> BuscarOuFalharAsync(Guid id, CancellationToken cancelamento)
        => await repositorio.ObterPorIdAsync(id, cancelamento)
           ?? throw new ExcecaoNaoEncontrado($"Produto {id} não encontrado.");

    private static ProdutoResposta Mapear(Produto p)
        => new(p.Id, p.Codigo, p.Descricao, p.Saldo);
}
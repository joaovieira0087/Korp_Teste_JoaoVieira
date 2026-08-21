using Korp.SharedKernel.Excecoes;

namespace Korp.Faturamento.Application.NotasFiscais;

public sealed class ItemNotaFiscal
{
    public Guid Id { get; private set; }
    public Guid NotaFiscalId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string CodigoProduto { get; private set; } = null!;
    public string DescricaoProduto { get; private set; } = null!;
    public int Quantidade { get; private set; }

    private ItemNotaFiscal() { }

    // internal: só a própria NotaFiscal cria itens.
    internal ItemNotaFiscal(
        Guid notaFiscalId, Guid produtoId,
        string codigoProduto, string descricaoProduto, int quantidade)
    {
        if (produtoId == Guid.Empty)
            throw new ExcecaoRegraDeNegocio("O produto do item é obrigatório.");

        Id               = Guid.CreateVersion7();
        NotaFiscalId     = notaFiscalId;
        ProdutoId        = produtoId;
        CodigoProduto    = codigoProduto?.Trim().ToUpperInvariant() ?? string.Empty;
        DescricaoProduto = descricaoProduto?.Trim() ?? string.Empty;

        if (quantidade <= 0)
            throw new ExcecaoRegraDeNegocio("A quantidade deve ser maior que zero.");

        Quantidade = quantidade;
    }

    internal void SomarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
            throw new ExcecaoRegraDeNegocio("A quantidade deve ser maior que zero.");

        Quantidade += quantidade;
    }
}
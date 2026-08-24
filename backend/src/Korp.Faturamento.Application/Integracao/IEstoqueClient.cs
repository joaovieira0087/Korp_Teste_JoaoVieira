namespace Korp.Faturamento.Application.Integracao;

public sealed record ProdutoEstoque(Guid Id, string Codigo, string Descricao, int Saldo);

public sealed record ItemBaixa(Guid ProdutoId, int Quantidade);

public sealed record BaixaEstoqueRequisicao(Guid NotaFiscalId, IReadOnlyList<ItemBaixa> Itens);

public sealed record ResumoEstoque(int TotalProdutos, int SaldoTotal, int ProdutosSemEstoque);

public interface IEstoqueClient
{
    Task<ProdutoEstoque> ObterProdutoAsync(Guid produtoId, CancellationToken cancelamento);

    Task BaixarAsync(BaixaEstoqueRequisicao requisicao, CancellationToken cancelamento);

    /// <summary>
    /// Retorna null se o estoque estiver indisponível. NUNCA lança —
    /// o dashboard degrada parcialmente em vez de falhar inteiro.
    /// </summary>
    Task<ResumoEstoque?> ObterResumoAsync(CancellationToken cancelamento);
}
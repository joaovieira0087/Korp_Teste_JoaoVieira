namespace Korp.Estoque.Application.Baixas;

public sealed record ItemBaixaRequisicao(Guid ProdutoId, int Quantidade);

public sealed record BaixaRequisicao(
    Guid NotaFiscalId, IReadOnlyList<ItemBaixaRequisicao> Itens);

public sealed record ItemBaixaResposta(
    Guid ProdutoId, string CodigoProduto, int SaldoAnterior, int SaldoAtual);

public sealed record BaixaResposta(
    Guid NotaFiscalId, IReadOnlyList<ItemBaixaResposta> Itens);
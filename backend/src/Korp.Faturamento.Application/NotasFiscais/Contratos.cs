namespace Korp.Faturamento.Application.NotasFiscais;

public sealed record ItemRequisicao(Guid ProdutoId, int Quantidade);

public sealed record CriarNotaFiscalRequisicao(IReadOnlyList<ItemRequisicao>? Itens);

public sealed record ItemResposta(
    Guid ProdutoId, string CodigoProduto, string DescricaoProduto, int Quantidade);

public sealed record NotaFiscalResposta(
    Guid Id,
    int Numero,
    string Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset? FechadaEm,
    IReadOnlyList<ItemResposta> Itens,
    int QuantidadeTotal);
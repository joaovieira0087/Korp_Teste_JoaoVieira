namespace Korp.Estoque.Application.Produtos;

public sealed record CriarProdutoRequisicao(string Codigo, string Descricao, int Saldo);

public sealed record AtualizarProdutoRequisicao(string Descricao, int Saldo);

public sealed record ProdutoResposta(Guid Id, string Codigo, string Descricao, int Saldo);
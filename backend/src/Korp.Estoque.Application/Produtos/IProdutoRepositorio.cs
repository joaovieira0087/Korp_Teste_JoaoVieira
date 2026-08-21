namespace Korp.Estoque.Application.Produtos;

public interface IProdutoRepositorio
{
    Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);
    Task<IReadOnlyList<Produto>> ListarAsync(string? filtro, CancellationToken cancelamento);
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancelamento);
    Task AdicionarAsync(Produto produto, CancellationToken cancelamento);
    void Remover(Produto produto);
    Task SalvarAlteracoesAsync(CancellationToken cancelamento);
}
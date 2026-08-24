namespace Korp.Estoque.Application.Produtos;
using Korp.Estoque.Application.Baixas;

public interface IProdutoRepositorio
{
    Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    Task<IReadOnlyList<Produto>> ListarAsync(string? filtro, CancellationToken cancelamento);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancelamento);
    
    Task AdicionarAsync(Produto produto, CancellationToken cancelamento);
    void Remover(Produto produto);

    Task SalvarAlteracoesAsync(CancellationToken cancelamento);

    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> identificadores, CancellationToken cancelamento);

    Task<IReadOnlyList<Produto>> ObterPorIdsComBloqueioAsync(IReadOnlyCollection<Guid> identificadores, CancellationToken cancelamento);

    Task<BaixaProcessada?> ObterBaixaProcessadaAsync(Guid notaFiscalId, CancellationToken cancelamento);

    Task RegistrarBaixaAsync(BaixaProcessada baixa, CancellationToken cancelamento);

    Task<ResumoEstoqueResposta> ObterResumoAsync(CancellationToken cancelamento);
}
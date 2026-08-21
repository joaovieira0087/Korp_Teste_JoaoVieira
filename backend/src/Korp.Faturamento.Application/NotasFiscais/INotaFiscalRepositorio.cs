namespace Korp.Faturamento.Application.NotasFiscais;

public interface INotaFiscalRepositorio
{
    Task<int> ProximoNumeroAsync(CancellationToken cancelamento);

    Task<NotaFiscal?> ObterPorIdAsync(Guid id, CancellationToken cancelamento);

    Task<IReadOnlyList<NotaFiscal>> ListarAsync(
        StatusNotaFiscal? status, CancellationToken cancelamento);

    Task AdicionarAsync(NotaFiscal nota, CancellationToken cancelamento);

    Task SalvarAlteracoesAsync(CancellationToken cancelamento);
}
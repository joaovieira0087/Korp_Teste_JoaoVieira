using Korp.Faturamento.Application.NotasFiscais;
using Microsoft.EntityFrameworkCore;

namespace Korp.Faturamento.Infrastructure.Persistencia;

public sealed class NotaFiscalRepositorio(FaturamentoDbContext contexto)
    : INotaFiscalRepositorio
{
    public async Task<int> ProximoNumeroAsync(CancellationToken cancelamento)
        => await contexto.Database
            .SqlQuery<int>($"SELECT nextval('seq_nota_fiscal_numero')::int AS \"Value\"")
            .SingleAsync(cancelamento);

    public async Task<NotaFiscal?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
        => await contexto.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id, cancelamento);

    public async Task<IReadOnlyList<NotaFiscal>> ListarAsync(
        StatusNotaFiscal? status, CancellationToken cancelamento)
    {
        var consulta = contexto.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Itens)
            .AsQueryable();

        if (status is not null)
            consulta = consulta.Where(n => n.Status == status);

        return await consulta
            .OrderByDescending(n => n.Numero)
            .ToListAsync(cancelamento);
    }

    public async Task AdicionarAsync(NotaFiscal nota, CancellationToken cancelamento)
        => await contexto.NotasFiscais.AddAsync(nota, cancelamento);

    public Task SalvarAlteracoesAsync(CancellationToken cancelamento)
        => contexto.SaveChangesAsync(cancelamento);
}
using Korp.Faturamento.Application.NotasFiscais;
using Microsoft.EntityFrameworkCore;

namespace Korp.Faturamento.Infrastructure.Persistencia;

public sealed class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> opcoes)
    : DbContext(opcoes)
{
    public const string SequenciaNumero = "seq_nota_fiscal_numero";

    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        construtor.HasDefaultSchema("public");

        construtor.HasSequence<int>(SequenciaNumero)
            .StartsAt(1)
            .IncrementsBy(1);

        construtor.ApplyConfigurationsFromAssembly(typeof(FaturamentoDbContext).Assembly);
    }
}
using Korp.Faturamento.Application.NotasFiscais;
using Korp.Faturamento.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Korp.Faturamento.Infrastructure;

public static class InjecaoDependencia
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddDbContext<FaturamentoDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("Postgres")));

        servicos.AddScoped<INotaFiscalRepositorio, NotaFiscalRepositorio>();

        return servicos;
    }
}
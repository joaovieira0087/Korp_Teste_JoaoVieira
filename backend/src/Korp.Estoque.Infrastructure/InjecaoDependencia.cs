using Korp.Estoque.Application.Produtos;
using Korp.Estoque.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Korp.Estoque.Infrastructure;

public static class InjecaoDependencia
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddDbContext<EstoqueDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("Postgres")));

        servicos.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();

        return servicos;
    }
}
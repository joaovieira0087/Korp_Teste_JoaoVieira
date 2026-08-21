using Korp.Estoque.Application.Produtos;
using Korp.Estoque.Application.Baixas;
using Microsoft.EntityFrameworkCore;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> opcoes)
    : DbContext(opcoes)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<BaixaProcessada> BaixasProcessadas => Set<BaixaProcessada>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        construtor.HasDefaultSchema("public");
        construtor.ApplyConfigurationsFromAssembly(typeof(EstoqueDbContext).Assembly);
    }
}
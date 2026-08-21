using Korp.Estoque.Application.Produtos;
using Microsoft.EntityFrameworkCore;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> opcoes)
    : DbContext(opcoes)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        construtor.HasDefaultSchema("public");
        construtor.ApplyConfigurationsFromAssembly(typeof(EstoqueDbContext).Assembly);
    }
}
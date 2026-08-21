using Korp.Estoque.Application.Produtos;
using Microsoft.EntityFrameworkCore;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class ProdutoRepositorio(EstoqueDbContext contexto) : IProdutoRepositorio
{
    public async Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancelamento)
        => await contexto.Produtos.FirstOrDefaultAsync(p => p.Id == id, cancelamento);

    public async Task<IReadOnlyList<Produto>> ListarAsync(
        string? filtro, CancellationToken cancelamento)
    {
        var consulta = contexto.Produtos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var termo = $"%{filtro.Trim()}%";
            consulta = consulta.Where(p =>
                EF.Functions.ILike(p.Codigo, termo) ||
                EF.Functions.ILike(p.Descricao, termo));
        }

        return await consulta
            .OrderBy(p => p.Codigo)
            .ToListAsync(cancelamento);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancelamento)
        => contexto.Produtos.AnyAsync(p => p.Codigo == codigo, cancelamento);

    public async Task AdicionarAsync(Produto produto, CancellationToken cancelamento)
        => await contexto.Produtos.AddAsync(produto, cancelamento);

    public void Remover(Produto produto)
        => contexto.Produtos.Remove(produto);

    public Task SalvarAlteracoesAsync(CancellationToken cancelamento)
        => contexto.SaveChangesAsync(cancelamento);

    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(
       IReadOnlyCollection<Guid> identificadores, CancellationToken cancelamento)
       => await contexto.Produtos
           .Where(p => identificadores.Contains(p.Id))
           .ToListAsync(cancelamento);
}
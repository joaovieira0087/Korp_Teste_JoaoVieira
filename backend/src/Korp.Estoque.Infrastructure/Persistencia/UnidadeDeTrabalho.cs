using Korp.Estoque.Application.Comum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Korp.Estoque.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(EstoqueDbContext contexto) : IUnidadeDeTrabalho
{
    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken cancelamento)
        => new Transacao(await contexto.Database.BeginTransactionAsync(cancelamento));

    private sealed class Transacao(IDbContextTransaction transacao) : ITransacao
    {
        public Task ConfirmarAsync(CancellationToken cancelamento)
            => transacao.CommitAsync(cancelamento);

        public ValueTask DisposeAsync() => transacao.DisposeAsync();
    }
}
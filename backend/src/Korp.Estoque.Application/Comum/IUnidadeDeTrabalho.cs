namespace Korp.Estoque.Application.Comum;

public interface ITransacao : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancelamento);
}

public interface IUnidadeDeTrabalho
{
    Task<ITransacao> IniciarTransacaoAsync(CancellationToken cancelamento);
}
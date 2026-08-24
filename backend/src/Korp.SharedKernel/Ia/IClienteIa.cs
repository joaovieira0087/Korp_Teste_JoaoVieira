namespace Korp.SharedKernel.Ia;

public interface IClienteIa
{
    bool Disponivel { get; }

    Task<string?> GerarTextoAsync(
        string instrucao, string entrada, CancellationToken cancelamento);
}
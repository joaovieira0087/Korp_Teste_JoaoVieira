namespace Korp.Estoque.Application.Baixas;

/// <summary>
/// Registro de que a baixa de uma nota já foi executada, junto com a
/// resposta original. A chave é o próprio id da nota fiscal.
/// </summary>
public sealed class BaixaProcessada
{
    public Guid NotaFiscalId { get; private set; }
    public string RespostaJson { get; private set; } = null!;
    public DateTimeOffset ProcessadaEm { get; private set; }

    private BaixaProcessada() { }

    public BaixaProcessada(Guid notaFiscalId, string respostaJson)
    {
        NotaFiscalId = notaFiscalId;
        RespostaJson = respostaJson;
        ProcessadaEm = DateTimeOffset.UtcNow;
    }
}
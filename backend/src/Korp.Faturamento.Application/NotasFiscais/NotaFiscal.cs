using Korp.SharedKernel.Excecoes;

namespace Korp.Faturamento.Application.NotasFiscais;

public sealed class NotaFiscal
{
    private readonly List<ItemNotaFiscal> _itens = [];

    public Guid Id { get; private set; }
    public int Numero { get; private set; }
    public StatusNotaFiscal Status { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset? FechadaEm { get; private set; }

    public IReadOnlyCollection<ItemNotaFiscal> Itens => _itens.AsReadOnly();

    private NotaFiscal() { }

    public NotaFiscal(int numero)
    {
        if (numero <= 0)
            throw new ExcecaoRegraDeNegocio("A numeração da nota fiscal é inválida.");

        Id = Guid.CreateVersion7();
        Numero = numero;
        Status = StatusNotaFiscal.Aberta;
        CriadaEm = DateTimeOffset.UtcNow;
    }

    public void AdicionarItem(
        Guid produtoId, string codigoProduto, string descricaoProduto, int quantidade)
    {
        GarantirQueEstaAberta();

        var existente = _itens.FirstOrDefault(i => i.ProdutoId == produtoId);

        if (existente is not null)
        {
            existente.SomarQuantidade(quantidade);
            return;
        }

        _itens.Add(new ItemNotaFiscal(
            Id, produtoId, codigoProduto, descricaoProduto, quantidade));
    }

    public void RemoverItem(Guid produtoId)
    {
        GarantirQueEstaAberta();

        var item = _itens.FirstOrDefault(i => i.ProdutoId == produtoId)
            ?? throw new ExcecaoNaoEncontrado(
                $"O produto {produtoId} não está na nota fiscal {Numero}.");

        _itens.Remove(item);
    }

    public void Fechar()
    {
        GarantirQueEstaAberta();

        if (_itens.Count == 0)
            throw new ExcecaoRegraDeNegocio(
                "Não é possível fechar uma nota fiscal sem itens.");

        Status = StatusNotaFiscal.Fechada;
        FechadaEm = DateTimeOffset.UtcNow;
    }

    private void GarantirQueEstaAberta()
    {
        if (Status is not StatusNotaFiscal.Aberta)
            throw new ExcecaoConflito(
                $"A nota fiscal {Numero} está fechada e não pode ser alterada.");
    }
}
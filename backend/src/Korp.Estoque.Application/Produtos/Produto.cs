using Korp.SharedKernel.Excecoes;

namespace Korp.Estoque.Application.Produtos;

public sealed class Produto
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public int Saldo { get; private set; }

    private Produto() { }

    public Produto(string codigo, string descricao, int saldoInicial)
    {
        Id = Guid.CreateVersion7();
        DefinirCodigo(codigo);
        AlterarDescricao(descricao);
        AjustarSaldo(saldoInicial);
    }

    public void AlterarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ExcecaoRegraDeNegocio("A descrição do produto é obrigatória.");

        descricao = descricao.Trim();

        if (descricao.Length > 200)
            throw new ExcecaoRegraDeNegocio("A descrição deve ter no máximo 200 caracteres.");

        Descricao = descricao;
    }

    public void AjustarSaldo(int novoSaldo)
    {
        if (novoSaldo < 0)
            throw new ExcecaoRegraDeNegocio("O saldo não pode ser negativo.");

        Saldo = novoSaldo;
    }

    public void Debitar(int quantidade)
    {
        if (quantidade <= 0)
            throw new ExcecaoRegraDeNegocio("A quantidade a debitar deve ser maior que zero.");

        if (quantidade > Saldo)
            throw new ExcecaoConflito(
                $"Saldo insuficiente para o produto {Codigo}: " +
                $"disponível {Saldo}, solicitado {quantidade}.");

        Saldo -= quantidade;
    }

    private void DefinirCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ExcecaoRegraDeNegocio("O código do produto é obrigatório.");

        codigo = codigo.Trim().ToUpperInvariant();

        if (codigo.Length > 30)
            throw new ExcecaoRegraDeNegocio("O código deve ter no máximo 30 caracteres.");

        Codigo = codigo;
    }
}
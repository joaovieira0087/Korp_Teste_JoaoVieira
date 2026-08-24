using System.Globalization;
using Korp.Faturamento.Application.Integracao;
using Korp.Faturamento.Application.NotasFiscais;
using Korp.SharedKernel.Ia;

namespace Korp.Faturamento.Application.Dashboard;

public sealed record ResumoEstoqueDto(
    int TotalProdutos, int SaldoTotal, int ProdutosSemEstoque);

public sealed record DashboardResposta(
    MetricasFaturamento Faturamento,
    ResumoEstoqueDto? Estoque,
    bool EstoqueDisponivel,
    TextoGerado Resumo);

public sealed class ServicoDashboard(
    AnalistaNotas analista,
    IEstoqueClient estoqueClient,
    IClienteIa clienteIa)
{
    private const string Instrucao = """
        Você resume o estado de um sistema ERP industrial para a tela inicial.
        Escreva de 2 a 3 frases em português do Brasil contextualizando a
        saúde operacional do negócio.

        Regras:
        - Use SOMENTE os números fornecidos. Não invente valores nem datas.
        - Texto corrido, sem markdown, sem listas, sem títulos.
        - Não repita todos os números; interprete-os.
        - Se o estoque estiver indisponível, mencione isso em uma frase curta.
        """;

    public async Task<DashboardResposta> ObterAsync(CancellationToken cancelamento)
    {
        var faturamento = await analista.ObterMetricasAsync(cancelamento);
        var estoque = await estoqueClient.ObterResumoAsync(cancelamento);

        var gerado = await clienteIa.GerarTextoAsync(
            Instrucao, MontarFatos(faturamento, estoque), cancelamento);

        var resumo = gerado is null
            ? new TextoGerado(Deterministico(faturamento, estoque), "Fallback")
            : new TextoGerado(AnalistaNotas.Sanitizar(gerado), "IA");

        return new DashboardResposta(
            faturamento,
            estoque is null
                ? null
                : new ResumoEstoqueDto(
                    estoque.TotalProdutos, estoque.SaldoTotal, estoque.ProdutosSemEstoque),
            EstoqueDisponivel: estoque is not null,
            resumo);
    }

    private static string MontarFatos(
        MetricasFaturamento faturamento, ResumoEstoque? estoque)
    {
        var blocoEstoque = estoque is null
            ? "Estoque: servico indisponivel no momento."
            : $"""
               Produtos cadastrados: {estoque.TotalProdutos}
               Saldo total em estoque: {estoque.SaldoTotal} unidades
               Produtos sem estoque: {estoque.ProdutosSemEstoque}
               """;

        return $"""
            {blocoEstoque}
            Total de notas: {faturamento.TotalNotas}
            Notas fechadas: {faturamento.Fechadas}
            Notas abertas: {faturamento.Abertas}
            Unidades ja faturadas: {faturamento.UnidadesFaturadas}
            Unidades pendentes em notas abertas: {faturamento.UnidadesPendentes}
            Produto mais movimentado: {faturamento.ProdutoMaisMovimentado ?? "nenhum"} ({faturamento.QuantidadeDoProdutoTop} un)
            """;
    }

    private static string Deterministico(
        MetricasFaturamento faturamento, ResumoEstoque? estoque)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        var fraseEstoque = estoque is null
            ? "O serviço de estoque está indisponível, então os dados de produtos não puderam ser carregados."
            : $"O sistema possui {estoque.TotalProdutos} produto(s) cadastrado(s) " +
              $"com saldo total de {estoque.SaldoTotal} unidades" +
              (estoque.ProdutosSemEstoque > 0
                  ? $", sendo {estoque.ProdutosSemEstoque} sem saldo disponível."
                  : ".");

        if (faturamento.TotalNotas == 0)
            return $"{fraseEstoque} Ainda não há notas fiscais registradas.";

        var taxa = 100.0 * faturamento.Fechadas / faturamento.TotalNotas;

        var frasePendencia = faturamento.UnidadesPendentes > 0
            ? $" Restam {faturamento.UnidadesPendentes} unidades pendentes em notas abertas, " +
              "que ainda impactarão o estoque na impressão."
            : " Não há unidades pendentes em notas abertas.";

        return $"{fraseEstoque} No faturamento, {faturamento.Fechadas} das " +
               $"{faturamento.TotalNotas} notas já foram encerradas " +
               $"({taxa.ToString("N0", cultura)}%), somando " +
               $"{faturamento.UnidadesFaturadas} unidades faturadas." + frasePendencia;
    }
}
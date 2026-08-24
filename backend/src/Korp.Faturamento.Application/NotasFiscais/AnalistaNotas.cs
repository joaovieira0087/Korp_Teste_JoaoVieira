using System.Globalization;
using Korp.SharedKernel.Excecoes;
using Korp.SharedKernel.Ia;

namespace Korp.Faturamento.Application.NotasFiscais;

public sealed record TextoGerado(string Texto, string Origem);

public sealed record MetricasFaturamento(
    int TotalNotas,
    int Abertas,
    int Fechadas,
    int UnidadesFaturadas,
    int UnidadesPendentes,
    string? ProdutoMaisMovimentado,
    int QuantidadeDoProdutoTop);

public sealed record AnaliseResposta(MetricasFaturamento Metricas, TextoGerado Analise);

public sealed class AnalistaNotas(
    INotaFiscalRepositorio repositorio, IClienteIa clienteIa)
{
    private const string InstrucaoResumo = """
        Você analisa notas fiscais de um ERP industrial.
        Escreva um resumo executivo em português do Brasil, de 2 a 3 frases,
        descrevendo a composição da nota e seu impacto no estoque.

        Regras:
        - Use SOMENTE os números fornecidos. Não invente valores, preços ou datas.
        - Texto corrido, sem markdown, sem listas, sem títulos.
        - Tom profissional e objetivo.
        """;

    private const string InstrucaoAnalise = """
        Você analisa o fluxo de faturamento de um ERP industrial.
        A partir das métricas fornecidas, escreva um diagnóstico de 3 a 4 frases
        em português do Brasil, apontando o estado do fluxo e um alerta prático
        se houver notas pendentes relevantes.

        Regras:
        - Use SOMENTE os números fornecidos. Não invente nada.
        - Texto corrido, sem markdown, sem listas.
        - Não repita todos os números; interprete-os.
        """;

    public async Task<TextoGerado> ResumirNotaAsync(
        Guid id, CancellationToken cancelamento)
    {
        var nota = await repositorio.ObterPorIdAsync(id, cancelamento)
            ?? throw new ExcecaoNaoEncontrado($"Nota fiscal {id} não encontrada.");

        var fatos = MontarFatosDaNota(nota);
        var gerado = await clienteIa.GerarTextoAsync(InstrucaoResumo, fatos, cancelamento);

        return gerado is null
            ? new TextoGerado(ResumoDeterministico(nota), "Fallback")
            : new TextoGerado(Sanitizar(gerado), "IA");
    }

    public async Task<AnaliseResposta> AnalisarHistoricoAsync(
        CancellationToken cancelamento)
    {
        var notas = await repositorio.ListarAsync(null, cancelamento);
        var metricas = CalcularMetricas(notas);

        var gerado = await clienteIa.GerarTextoAsync(
            InstrucaoAnalise, MontarFatosDoHistorico(metricas), cancelamento);

        var analise = gerado is null
            ? new TextoGerado(AnaliseDeterministica(metricas), "Fallback")
            : new TextoGerado(Sanitizar(gerado), "IA");

        return new AnaliseResposta(metricas, analise);
    }


    private static MetricasFaturamento CalcularMetricas(IReadOnlyList<NotaFiscal> notas)
    {
        var topProduto = notas
            .SelectMany(n => n.Itens)
            .GroupBy(i => i.CodigoProduto)
            .Select(g => new { Codigo = g.Key, Quantidade = g.Sum(i => i.Quantidade) })
            .OrderByDescending(x => x.Quantidade)
            .FirstOrDefault();

        return new MetricasFaturamento(
            TotalNotas: notas.Count,
            Abertas: notas.Count(n => n.Status == StatusNotaFiscal.Aberta),
            Fechadas: notas.Count(n => n.Status == StatusNotaFiscal.Fechada),
            UnidadesFaturadas: notas.Where(n => n.Status == StatusNotaFiscal.Fechada)
                                    .SelectMany(n => n.Itens).Sum(i => i.Quantidade),
            UnidadesPendentes: notas.Where(n => n.Status == StatusNotaFiscal.Aberta)
                                    .SelectMany(n => n.Itens).Sum(i => i.Quantidade),
            ProdutoMaisMovimentado: topProduto?.Codigo,
            QuantidadeDoProdutoTop: topProduto?.Quantidade ?? 0);
    }

    private static string MontarFatosDaNota(NotaFiscal nota)
    {
        var itens = string.Join("; ", nota.Itens
            .OrderByDescending(i => i.Quantidade)
            .Select(i => $"{i.CodigoProduto} ({i.DescricaoProduto}): {i.Quantidade} un"));

        return $"""
            Numero da nota: {nota.Numero}
            Status: {nota.Status}
            Criada em: {nota.CriadaEm:dd/MM/yyyy}
            Produtos distintos: {nota.Itens.Count}
            Unidades no total: {nota.Itens.Sum(i => i.Quantidade)}
            Itens: {(itens.Length == 0 ? "nenhum" : itens)}
            """;
    }

    private static string MontarFatosDoHistorico(MetricasFaturamento m)
        => $"""
            Total de notas: {m.TotalNotas}
            Notas abertas: {m.Abertas}
            Notas fechadas: {m.Fechadas}
            Unidades ja faturadas (notas fechadas): {m.UnidadesFaturadas}
            Unidades pendentes (notas abertas): {m.UnidadesPendentes}
            Produto mais movimentado: {m.ProdutoMaisMovimentado ?? "nenhum"} ({m.QuantidadeDoProdutoTop} un)
            """;

    private static string ResumoDeterministico(NotaFiscal nota)
    {
        if (nota.Itens.Count == 0)
            return $"A nota fiscal {nota.Numero} está {nota.Status.ToString().ToLowerInvariant()} " +
                   "e ainda não possui itens.";

        var unidades = nota.Itens.Sum(i => i.Quantidade);
        var principal = nota.Itens.MaxBy(i => i.Quantidade)!;
        var percentual = 100.0 * principal.Quantidade / unidades;

        var impacto = nota.Status == StatusNotaFiscal.Fechada
            ? "O estoque já foi baixado."
            : "O estoque ainda não foi baixado; a baixa ocorre na impressão.";

        return $"A nota fiscal {nota.Numero} está {nota.Status.ToString().ToLowerInvariant()} " +
               $"e reúne {nota.Itens.Count} produto(s) totalizando {unidades} unidades. " +
               $"O item de maior volume é {principal.CodigoProduto}, com {principal.Quantidade} " +
               $"unidades ({percentual.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"))}% do total). " +
               impacto;
    }

    private static string AnaliseDeterministica(MetricasFaturamento m)
    {
        if (m.TotalNotas == 0)
            return "Ainda não há notas fiscais registradas para analisar.";

        var taxa = 100.0 * m.Fechadas / m.TotalNotas;
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        var alerta = m.UnidadesPendentes > m.UnidadesFaturadas
            ? " Atenção: há mais unidades pendentes em notas abertas do que já faturadas, " +
              "o que representa um impacto de estoque ainda não realizado."
            : string.Empty;

        return $"Foram registradas {m.TotalNotas} notas, sendo {m.Fechadas} fechadas " +
               $"({taxa.ToString("N0", cultura)}% do total) e {m.Abertas} ainda abertas. " +
               $"Já foram faturadas {m.UnidadesFaturadas} unidades, com " +
               $"{m.UnidadesPendentes} pendentes. " +
               $"O produto mais movimentado é {m.ProdutoMaisMovimentado ?? "n/d"} " +
               $"com {m.QuantidadeDoProdutoTop} unidades." + alerta;
    }

    private static string Sanitizar(string texto)
    {
        var limpo = texto
            .Replace("**", string.Empty).Replace("*", string.Empty)
            .Replace("#", string.Empty).Trim();

        return limpo.Length <= 900 ? limpo : limpo[..897].TrimEnd() + "...";
    }
}
using System.Globalization;
using Korp.SharedKernel.Ia;
using Korp.SharedKernel.Excecoes;

namespace Korp.Estoque.Application.Produtos;

public sealed record SugestaoDescricaoRequisicao(string TextoBase);

public sealed record SugestaoDescricaoResposta(string Descricao, string Origem);

public sealed class AssistenteDescricao(IClienteIa clienteIa)
{
	private const string Instrucao = """
        Você é um assistente de cadastro de produtos de um sistema ERP industrial.
        A partir de um texto curto informado pelo usuário, escreva UMA descrição
        técnica de produto, em português do Brasil.

        Regras:
        - No máximo 200 caracteres.
        - Uma única linha, sem quebras, sem aspas, sem markdown.
        - Sem preço, sem quantidade, sem marca inventada.
        - Se o texto for vago demais, apenas normalize o que foi informado.
        - Responda somente com a descrição, sem nenhum comentário adicional.
        """;

	public async Task<SugestaoDescricaoResposta> SugerirAsync(
		SugestaoDescricaoRequisicao requisicao, CancellationToken cancelamento)
	{
		var textoBase = requisicao.TextoBase?.Trim() ?? string.Empty;

		if (textoBase.Length == 0)
			throw new ExcecaoRegraDeNegocio(
				"Informe um texto base para o assistente trabalhar.");

		var gerado = await clienteIa.GerarTextoAsync(Instrucao, textoBase, cancelamento);

		return gerado is null
			? new SugestaoDescricaoResposta(Normalizar(textoBase), "Fallback")
			: new SugestaoDescricaoResposta(Sanitizar(gerado), "IA");
	}

	private static string Normalizar(string texto)
	{
		var limpo = string.Join(' ', texto.Split(
			' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

		var capitalizado = CultureInfo.GetCultureInfo("pt-BR").TextInfo
			.ToTitleCase(limpo.ToLowerInvariant());

		return Truncar(capitalizado);
	}

	private static string Sanitizar(string texto)
	{
		var linha = texto
			.Replace("\r", " ").Replace("\n", " ")
			.Replace("\"", string.Empty).Replace("*", string.Empty)
			.Trim();

		linha = string.Join(' ', linha.Split(
			' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

		return Truncar(linha);
	}

	private static string Truncar(string texto)
		=> texto.Length <= 200 ? texto : texto[..197].TrimEnd() + "...";
}
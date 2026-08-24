using Korp.Faturamento.Application.NotasFiscais;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Faturamento.Api.Controllers;

[ApiController]
[Route("api/notas-fiscais")]
[Produces("application/json")]
public sealed class NotasFiscaisController(ServicoNotasFiscais servico) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotaFiscalResposta>>> Listar(
        [FromQuery] StatusNotaFiscal? status, CancellationToken cancelamento)
        => Ok(await servico.ListarAsync(status, cancelamento));

    [HttpGet("{id:guid}", Name = nameof(ObterPorId))]
    public async Task<ActionResult<NotaFiscalResposta>> ObterPorId(
        Guid id, CancellationToken cancelamento)
        => Ok(await servico.ObterPorIdAsync(id, cancelamento));

    [HttpPost]
    public async Task<ActionResult<NotaFiscalResposta>> Criar(
        CriarNotaFiscalRequisicao requisicao, CancellationToken cancelamento)
    {
        var nota = await servico.CriarAsync(requisicao, cancelamento);
        return CreatedAtRoute(nameof(ObterPorId), new { id = nota.Id }, nota);
    }

    [HttpPost("{id:guid}/itens")]
    public async Task<ActionResult<NotaFiscalResposta>> AdicionarItem(
        Guid id, ItemRequisicao item, CancellationToken cancelamento)
        => Ok(await servico.AdicionarItemAsync(id, item, cancelamento));

    [HttpDelete("{id:guid}/itens/{produtoId:guid}")]
    public async Task<ActionResult<NotaFiscalResposta>> RemoverItem(
        Guid id, Guid produtoId, CancellationToken cancelamento)
        => Ok(await servico.RemoverItemAsync(id, produtoId, cancelamento));

    [HttpPost("{id:guid}/imprimir")]
    public async Task<ActionResult<NotaFiscalResposta>> Imprimir(
        Guid id, CancellationToken cancelamento)
        => Ok(await servico.ImprimirAsync(id, cancelamento));

    [HttpGet("analise")]
    public async Task<ActionResult<AnaliseResposta>> Analisar(
    [FromServices] AnalistaNotas analista, CancellationToken cancelamento)
    => Ok(await analista.AnalisarHistoricoAsync(cancelamento));

    [HttpGet("{id:guid}/resumo")]
    public async Task<ActionResult<TextoGerado>> Resumir(
        [FromServices] AnalistaNotas analista, Guid id, CancellationToken cancelamento)
        => Ok(await analista.ResumirNotaAsync(id, cancelamento));
}
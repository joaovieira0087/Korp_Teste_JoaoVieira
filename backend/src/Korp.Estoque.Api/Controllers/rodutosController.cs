using Korp.Estoque.Application.Produtos;
using Microsoft.AspNetCore.Mvc;


namespace Korp.Estoque.Api.Controllers;

[ApiController]
[Route("api/produtos")]
[Produces("application/json")]
public sealed class ProdutosController(ServicoProdutos servico) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProdutoResposta>>> Listar(
        [FromQuery] string? filtro, CancellationToken cancelamento)
        => Ok(await servico.ListarAsync(filtro, cancelamento));

    [HttpGet("{id:guid}", Name = nameof(ObterPorId))]
    public async Task<ActionResult<ProdutoResposta>> ObterPorId(
        Guid id, CancellationToken cancelamento)
        => Ok(await servico.ObterPorIdAsync(id, cancelamento));

    [HttpPost]
    public async Task<ActionResult<ProdutoResposta>> Criar(
        CriarProdutoRequisicao requisicao, CancellationToken cancelamento)
    {
        var produto = await servico.CriarAsync(requisicao, cancelamento);
        return CreatedAtRoute(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProdutoResposta>> Atualizar(
        Guid id, AtualizarProdutoRequisicao requisicao, CancellationToken cancelamento)
        => Ok(await servico.AtualizarAsync(id, requisicao, cancelamento));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancelamento)
    {
        await servico.ExcluirAsync(id, cancelamento);
        return NoContent();
    }

    [HttpPost("assistente/descricao")]
    public async Task<ActionResult<SugestaoDescricaoResposta>> SugerirDescricao(
    [FromServices] AssistenteDescricao assistente,
    SugestaoDescricaoRequisicao requisicao,
    CancellationToken cancelamento)
    => Ok(await assistente.SugerirAsync(requisicao, cancelamento));

    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoEstoqueResposta>> Resumo(
    [FromServices] IProdutoRepositorio repositorio, CancellationToken cancelamento)
    => Ok(await repositorio.ObterResumoAsync(cancelamento));
}
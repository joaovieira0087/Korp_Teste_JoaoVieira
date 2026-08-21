using Korp.Estoque.Application.Baixas;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Estoque.Api.Controllers;

[ApiController]
[Route("api/estoque/baixas")]
[Produces("application/json")]
public sealed class BaixasController(ServicoBaixas servico) : ControllerBase
{   
    [HttpPost]
    public async Task<ActionResult<BaixaResposta>> Executar(
        BaixaRequisicao requisicao, CancellationToken cancelamento)
        => Ok(await servico.ExecutarAsync(requisicao, cancelamento));
}
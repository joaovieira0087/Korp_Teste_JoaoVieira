using Korp.Faturamento.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Faturamento.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(ServicoDashboard servico) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResposta>> Obter(
        CancellationToken cancelamento)
        => Ok(await servico.ObterAsync(cancelamento));
}